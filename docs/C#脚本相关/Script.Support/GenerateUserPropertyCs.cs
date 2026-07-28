using System;
using System.Collections.Generic;
using System.Text;

namespace Script.Support
{
	// Token: 0x02000005 RID: 5
	public class GenerateUserPropertyCs
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600002F RID: 47 RVA: 0x00002DE5 File Offset: 0x00000FE5
		public static GenerateUserPropertyCs Instance
		{
			get
			{
				return GenerateUserPropertyCs._instance;
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002DF8 File Offset: 0x00000FF8
		public string GeneralPropertyCode(List<IOInfo> inputIo, List<IOInfo> outputIo)
		{
			string text = string.Empty;
			if (inputIo == null || outputIo == null || inputIo.Count <= 0 || outputIo.Count <= 0)
			{
				return text;
			}
			List<string> list = new List<string>();
			this.GetPorpertyList(inputIo, list, true);
			this.GetPorpertyList(outputIo, list, false);
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string value in list)
			{
				stringBuilder.Append(value).Append("\r\n\r\n");
			}
			text = this.CodeTemplate;
			return text.Replace("{property}", stringBuilder.ToString());
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002EAC File Offset: 0x000010AC
		private void GetPorpertyList(List<IOInfo> Ios, List<string> PropertyList, bool isInput = false)
		{
			string newValue = "";
			foreach (IOInfo ioinfo in Ios)
			{
				string text;
				if (isInput)
				{
					text = this.GetInputTemplate(ioinfo.IoType);
				}
				else
				{
					text = this.GetOutputTemplate(ioinfo.IoType);
				}
				string ioType;
				switch (ioType = ioinfo.IoType)
				{
				case "int":
					text = text.Replace("{type}", ioinfo.IoType).Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Int");
					newValue = "0";
					break;
				case "int[]":
					text = text.Replace("{type}", "int[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Int");
					newValue = "new int[]{}";
					break;
				case "float":
					text = text.Replace("{type}", ioinfo.IoType).Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Float");
					newValue = "0f";
					break;
				case "float[]":
					text = text.Replace("{type}", "float[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Float");
					newValue = "new float[]{}";
					break;
				case "string":
					text = text.Replace("{type}", ioinfo.IoType).Replace("{name}", ioinfo.IoName).Replace("{typeName}", "String");
					newValue = "string.Empty";
					break;
				case "string[]":
					text = text.Replace("{type}", "string[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "String");
					newValue = "new string[]{}";
					break;
				case "byte":
					text = text.Replace("{type}", "byte[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Bytes");
					newValue = "new byte[]{}";
					break;
				case "image":
					text = text.Replace("{type}", "ImageData").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Image");
					newValue = "new ImageData()";
					break;
				case "roibox":
					text = text.Replace("{type}", "RoiboxData").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Roibox");
					newValue = "new RoiboxData()";
					break;
				case "roibox[]":
					text = text.Replace("{type}", "RoiboxData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "RoiBox");
					newValue = "new RoiboxData[]{}";
					break;
				case "roiannulus":
					text = text.Replace("{type}", "AnnulusData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Annulus");
					newValue = "new AnnulusData[]{}";
					break;
				case "roipolygon":
					text = text.Replace("{type}", "PolygonData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Polygon");
					newValue = "new PolygonData[]{}";
					break;
				case "point":
					text = text.Replace("{type}", "PointData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Point");
					newValue = "new PointData[]{}";
					break;
				case "line":
					text = text.Replace("{type}", "LineData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Line");
					newValue = "new LineData[]{}";
					break;
				case "fixture":
					text = text.Replace("{type}", "FixtureData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Fixture");
					newValue = "new FixtureData[]{}";
					break;
				case "circle":
					text = text.Replace("{type}", "CircleData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Circle");
					newValue = "new CircleData[]{}";
					break;
				case "rect":
					text = text.Replace("{type}", "RectData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Rect");
					newValue = "new RectData[]{}";
					break;
				case "ellipse":
					text = text.Replace("{type}", "EllipseData[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "Ellipse");
					newValue = "new EllipseData[]{}";
					break;
				case "pointset":
					text = text.Replace("{type}", "byte[]").Replace("{name}", ioinfo.IoName).Replace("{typeName}", "ContourPoint");
					newValue = "new byte[]{}";
					break;
				}
				if (isInput)
				{
					text = text.Replace("{defaultvalue}", newValue);
				}
				PropertyList.Add(text);
			}
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003524 File Offset: 0x00001724
		private string GetInputTemplate(string type)
		{
			if (type == "int" || type == "float" || type == "string" || type == "byte" || type == "image" || type == "roibox")
			{
				return this.InputTemplate;
			}
			return this.InputTemplateV2;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x0000358C File Offset: 0x0000178C
		private string GetOutputTemplate(string type)
		{
			if (type == "int" || type == "float" || type == "string" || type == "byte" || type == "image" || type == "roibox")
			{
				return this.OutputTemplate;
			}
			return this.OutputTemplateV2;
		}

		// Token: 0x04000014 RID: 20
		private static readonly GenerateUserPropertyCs _instance = new GenerateUserPropertyCs();

		// Token: 0x04000015 RID: 21
		private string CodeTemplate = "using System;\r\nusing System.Text;\r\nusing System.Windows.Forms;\r\nusing Script.Methods;\r\nusing Conceal;\r\npublic partial class UserScript:ScriptMethods,IProcessMethods\r\n{\r\n    \r\n{property}\r\n\r\n}";

		// Token: 0x04000016 RID: 22
		private string InputTemplate = "    public {type} {name}\r\n    {\r\n        get\r\n        {\r\n            {type} tmp = {defaultvalue};\r\n            nErrorCode = Get{typeName}Value(\"{name}\", ref tmp);\r\n            return tmp;\r\n        }\r\n    }";

		// Token: 0x04000017 RID: 23
		private string OutputTemplate = "    public {type} {name}\r\n    {\r\n        set\r\n        {\r\n            nErrorCode = Set{typeName}Value(\"{name}\", value);\r\n        }\r\n    }";

		// Token: 0x04000018 RID: 24
		private string InputTemplateV2 = "    public {type} {name}\r\n    {\r\n        get\r\n        {\r\n            {type} tmp = {defaultvalue};\r\n            nErrorCode = (InternalObject as InternalMethods).Get{typeName}ArrayValue(\"{name}\", ref tmp);\r\n            return tmp;\r\n        }\r\n    }";

		// Token: 0x04000019 RID: 25
		private string OutputTemplateV2 = "    public {type} {name}\r\n    {\r\n        set\r\n        {\r\n            nErrorCode = (InternalObject as InternalMethods).Set{typeName}ArrayValue(\"{name}\", value);\r\n        }\r\n    }";
	}
}
