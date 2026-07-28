using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace VmScriptCompiler.Core;

public sealed record ModuleBinaryParameterWrite(string Procedure, string Module, string Parameter, byte[] Value);

public static class ModuleFrameCompatibility
{
    private const string EntryName = "SolutionFile/MoudleFrame";

    public static void RepairVersion4WriterOutput(string baseSolution, string outputSolution)
    {
        if (ReadVersion(baseSolution) != 4) return;
        using var archive = ZipFile.Open(outputSolution, ZipArchiveMode.Update);
        var entry = archive.Entries.Single(x => Normalize(x.FullName) == EntryName);
        byte[] malformed;
        using (var stream = entry.Open())
        using (var memory = new MemoryStream()) { stream.CopyTo(memory); malformed = memory.ToArray(); }
        var records = ReadWriterRecords(malformed);
        var repaired = WriteVersion4(records);
        var name = entry.FullName;
        entry.Delete();
        var replacement = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var output = replacement.Open();
        output.Write(repaired);
    }

    public static void AddOrReplaceBinaryParameters(string solution, IReadOnlyList<ModuleBinaryParameterWrite> writes)
    {
        if (writes.Count == 0) return;
        using var archive = ZipFile.Open(solution, ZipArchiveMode.Update);
        var frameEntry = archive.Entries.Single(x => Normalize(x.FullName) == EntryName);
        var serverEntry = archive.Entries.Single(x => Normalize(x.FullName) == "SolutionFile/VmServer.xml");
        var document = new XmlDocument();
        using (var input = serverEntry.Open()) document.Load(input);
        var moduleIds = writes.GroupBy(x => FindModuleIndex(document, x.Procedure, x.Module))
            .ToDictionary(x => x.Key, x => x.ToArray(), EqualityComparer<int>.Default);
        byte[] frame;
        using (var input = frameEntry.Open())
        using (var memory = new MemoryStream()) { input.CopyTo(memory); frame = memory.ToArray(); }
        var version = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(0, 4));
        var records = ReadRecords(frame, version);
        for (var index = 0; index < records.Count; index++)
        {
            var separator = records[index].Name.IndexOf('-');
            if (separator <= 0 || !int.TryParse(records[index].Name[..separator], out var moduleId) || !moduleIds.TryGetValue(moduleId, out var moduleWrites)) continue;
            var payload = records[index].Data;
            foreach (var write in moduleWrites) payload = SetBinaryParameter(payload, write.Parameter, write.Value);
            records[index] = (records[index].Name, payload);
            moduleIds.Remove(moduleId);
        }
        if (moduleIds.Count > 0) throw new CompilerException("MODULE_FRAME_INVALID", "Cannot locate ModuleFrame record for script compatibility parameters: " + string.Join(", ", moduleIds.Values.SelectMany(x => x).Select(x => x.Procedure + "." + x.Module).Distinct()));
        var rebuilt = WriteRecords(records, version);
        var entryName = frameEntry.FullName;
        frameEntry.Delete();
        var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var output = replacement.Open();
        output.Write(rebuilt);
    }

    private static int ReadVersion(string solution)
    {
        using var archive = ZipFile.OpenRead(solution);
        var entry = archive.Entries.Single(x => Normalize(x.FullName) == EntryName);
        Span<byte> header = stackalloc byte[4];
        using var stream = entry.Open();
        if (stream.Read(header) != 4) throw new CompilerException("MODULE_FRAME_INVALID", "MoudleFrame header is incomplete.");
        return BinaryPrimitives.ReadInt32BigEndian(header);
    }

    private static List<(string Name, byte[] Data)> ReadWriterRecords(byte[] bytes)
    {
        if (bytes.Length < 8 || BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(0, 4)) != 4)
            throw new CompilerException("MODULE_FRAME_INVALID", "Expected a version 4 ModuleFrame writer output.");
        var count = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4, 4));
        var offset = 8;
        var result = new List<(string, byte[])>(count);
        for (var index = 0; index < count; index++)
        {
            if (offset + 516 > bytes.Length) throw new CompilerException("MODULE_FRAME_INVALID", "Version 4 writer record is truncated.");
            var name = Encoding.UTF8.GetString(bytes, offset, 512).TrimEnd('\0');
            offset += 512;
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            if (length < 0 || offset + length > bytes.Length) throw new CompilerException("MODULE_FRAME_INVALID", "Version 4 writer payload length is invalid.");
            result.Add((name, bytes.AsSpan(offset, length).ToArray()));
            offset += length;
        }
        return result;
    }

    private static List<(string Name, byte[] Data)> ReadRecords(byte[] bytes, int version)
    {
        if (version == 4) return ReadVersion4Records(bytes);
        if (version != 7) throw new CompilerException("MODULE_FRAME_INVALID", "Unsupported ModuleFrame version for binary parameter update: " + version);
        if (bytes.Length < 8) throw new CompilerException("MODULE_FRAME_INVALID", "MoudleFrame header is incomplete.");
        var count = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4, 4));
        var offset = 8;
        var records = new List<(string, byte[])>(count);
        for (var index = 0; index < count; index++)
        {
            if (offset + 516 > bytes.Length) throw new CompilerException("MODULE_FRAME_INVALID", "ModuleFrame record is truncated.");
            var name = Encoding.UTF8.GetString(bytes, offset, 512).TrimEnd('\0');
            offset += 512;
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            if (length < 0 || offset + length > bytes.Length) throw new CompilerException("MODULE_FRAME_INVALID", "ModuleFrame payload length is invalid.");
            records.Add((name, bytes.AsSpan(offset, length).ToArray()));
            offset += length;
        }
        return records;
    }

    private static List<(string Name, byte[] Data)> ReadVersion4Records(byte[] bytes)
    {
        if (bytes.Length < 8) throw new CompilerException("MODULE_FRAME_INVALID", "MoudleFrame header is incomplete.");
        var count = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4, 4));
        var offset = 8;
        var records = new List<(string, byte[])>(count);
        for (var index = 0; index < count; index++)
        {
            if (offset + 260 > bytes.Length) throw new CompilerException("MODULE_FRAME_INVALID", "Version 4 ModuleFrame record is truncated.");
            var name = Encoding.Unicode.GetString(bytes, offset, 256).TrimEnd('\0');
            offset += 256;
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            if (length < 0 || offset + length > bytes.Length) throw new CompilerException("MODULE_FRAME_INVALID", "Version 4 ModuleFrame payload length is invalid.");
            records.Add((name, bytes.AsSpan(offset, length).ToArray()));
            offset += length;
        }
        return records;
    }

    private static byte[] WriteRecords(List<(string Name, byte[] Data)> records, int version)
    {
        if (version == 4) return WriteVersion4(records);
        var result = new byte[8 + records.Sum(x => 516 + x.Data.Length)];
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(0, 4), version);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(4, 4), records.Count);
        var offset = 8;
        foreach (var record in records)
        {
            var name = Encoding.UTF8.GetBytes(record.Name);
            name.AsSpan(0, Math.Min(name.Length, 512)).CopyTo(result.AsSpan(offset, 512));
            offset += 512;
            BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset, 4), record.Data.Length);
            offset += 4;
            record.Data.CopyTo(result, offset);
            offset += record.Data.Length;
        }
        return result;
    }

    private static byte[] SetBinaryParameter(byte[] payload, string parameter, byte[] value)
    {
        if (payload.Length < 12) throw new CompilerException("MODULE_FRAME_INVALID", "Script module payload is incomplete.");
        var algorithmCount = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4));
        var markerOffset = checked(4 + algorithmCount * 1284);
        if (algorithmCount < 0 || markerOffset + 8 > payload.Length || BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(markerOffset, 4)) != 1)
            throw new CompilerException("MODULE_FRAME_INVALID", "Script module binary parameter table is invalid.");
        var countOffset = markerOffset + 4;
        var count = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(countOffset, 4));
        var offset = countOffset + 4;
        var parameters = new List<(string Name, byte[] Value)>(count + 1);
        for (var index = 0; index < count; index++)
        {
            if (offset + 264 > payload.Length) throw new CompilerException("MODULE_FRAME_INVALID", "Binary parameter record is truncated.");
            var name = Encoding.UTF8.GetString(payload, offset, 260).TrimEnd('\0');
            offset += 260;
            var length = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(offset, 4));
            offset += 4;
            if (length < 0 || offset + length > payload.Length) throw new CompilerException("MODULE_FRAME_INVALID", "Binary parameter value length is invalid.");
            parameters.Add((name, payload.AsSpan(offset, length).ToArray()));
            offset += length;
        }
        var existing = parameters.FindIndex(x => x.Name == parameter);
        if (existing >= 0)
        {
            parameters[existing] = (parameter, value);
        }
        else if (parameter == "ShellRefrences")
        {
            // VM's ShellModule loader expects the optional reference table in its
            // canonical slot between Output and ShellContent. Appending the same
            // bytes at the end parses correctly but VM 4.4 ignores them during
            // precompile, causing referenced namespaces to appear missing.
            var shellContent = parameters.FindIndex(x => x.Name == "ShellContent");
            parameters.Insert(shellContent >= 0 ? shellContent : parameters.Count, (parameter, value));
        }
        else
        {
            parameters.Add((parameter, value));
        }
        var result = new byte[countOffset + 4 + parameters.Sum(x => 264 + x.Value.Length)];
        payload.AsSpan(0, countOffset).CopyTo(result);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(countOffset, 4), parameters.Count);
        offset = countOffset + 4;
        foreach (var item in parameters)
        {
            var name = Encoding.UTF8.GetBytes(item.Name);
            name.AsSpan(0, Math.Min(name.Length, 260)).CopyTo(result.AsSpan(offset, 260));
            offset += 260;
            BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset, 4), item.Value.Length);
            offset += 4;
            item.Value.CopyTo(result, offset);
            offset += item.Value.Length;
        }
        return result;
    }

    private static int FindModuleIndex(XmlDocument document, string procedureName, string moduleName)
    {
        var procedure = document.SelectNodes("/Root/ProceduresInfo/ProcedureBase/Procedure")!.OfType<XmlElement>()
            .SingleOrDefault(x => x.GetAttribute("DisplayName") == procedureName)
            ?? throw new CompilerException("PROCEDURE_NOT_FOUND", "VmServer.xml 中未找到流程: " + procedureName);
        var inside = document.SelectNodes("/Root/ProceduresInfo/ProcedureInsideModules/Procedure")!.OfType<XmlElement>()
            .Single(x => x.GetAttribute("Index") == procedure.GetAttribute("Index")).ChildNodes.OfType<XmlElement>()
            .Select(x => x.GetAttribute("Index")).ToHashSet(StringComparer.Ordinal);
        var module = document.SelectNodes("/Root/ModulesInfo/ModuleBase/Module")!.OfType<XmlElement>()
            .SingleOrDefault(x => x.GetAttribute("DisplayName") == moduleName && inside.Contains(x.GetAttribute("Index")))
            ?? throw new CompilerException("MODULE_NOT_FOUND", "VmServer.xml 中无法唯一定位模块: " + procedureName + "." + moduleName);
        return int.Parse(module.GetAttribute("Index"), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[] WriteVersion4(List<(string Name, byte[] Data)> records)
    {
        var length = 8 + records.Sum(x => 260 + x.Data.Length);
        var result = new byte[length];
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(0, 4), 4);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(4, 4), records.Count);
        var offset = 8;
        foreach (var record in records)
        {
            var name = Encoding.Unicode.GetBytes(record.Name);
            name.AsSpan(0, Math.Min(name.Length, 254)).CopyTo(result.AsSpan(offset, 256));
            offset += 256;
            BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset, 4), record.Data.Length);
            offset += 4;
            record.Data.CopyTo(result, offset);
            offset += record.Data.Length;
        }
        return result;
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
