# 脚本开放接口

> 路径：模块使用参考 > 逻辑工具 > 脚本 > 脚本开放接口

脚本提供开放接口，方便您通过几行代码快速实现流程控制和通信控制。

## 脚本开放接口概览

**表 1 脚本全量接口**

| 实现类型 | 方法 | 描述 |
| --- | --- | --- |
| 初始化 | Init | 初始化脚本 |
| 流程逻辑处理 | Process | 定义单个流程的执行逻辑 |
| 全局变量处理 | GlobalVariableModule.SetValue | 设置全局变量的值 |
| 全局变量处理 | GlobalVariableModule.GetValue | 获取全局变量的值 |
| 处理模块结果与参数 | CurrentProcess.GetModule | 获取模块结果数据 |
| 处理模块结果与参数 | CurrentProcess.GetModule.SetValue | 设置模块运行参数的值 |
| 处理模块结果与参数 | GetModuleParam | 获取模块运行参数的值 |
| 处理模块结果与参数 | BytesToPointset | 将点集二进制数据转换为轮廓点数组数据 |
| 处理模块结果与参数 | PointsetToBytes | 将轮廓点数组数据转换为点集二进制数据 |
| 发送通信数据 | SendData | 指定通信设备发送特定类型的数据 |
| 获取数据 | GetIntValue / GetFloatValue / GetStringValue / GetBytesValue / GetIMAGEValue / GetRoiboxValue / GetIntArrayValue / GetFloatArrayValue / GetStringArrayValue | 推荐使用变量名称方式获取输入数据 |
| 输出数据 | SetIntValue / SetIntArrayValue / SetFloatValue / SetFloatArrayValue / SetStringValue / SetStringArrayValue / SetBytesValue / SetImageValue / SetRoiboxValue / SetStringValueByIndex / SetIntValueByIndex / SetFloatValueByIndex | 推荐使用变量名称方式设置数据输出 |
| 渲染相关 | ShowImage | 在渲染控件上显示图像 |
| 渲染相关 | DrawShape | 在渲染控件上绘制图形 |
| 调试 | ConsoleWrite | 将信息打印至 DebugView |
| 调试 | ShowMessageBox | 将错误信息通过弹窗提示 |

### 获取数据示例（推荐用法）

推荐您在脚本中使用变量名称方式获取输入数据，假设您要获取输入变量名称为 `in0` 的 POINT 类型数据，代码示例如下：

> **说明：**
> - 使用 GetIntValue、GetRoiboxValue 等接口为上一代数据获取方式，出于方案兼容考虑，保留了这些接口，但不推荐使用。
> - 实际使用中，请根据您的变量名称和变量类型修改代码片段，其中不同变量类型在脚本中对应不同的数组类型，命名规则为 `<变量类型>Data[]`（适用变量类型为非数组类型），例如变量为 POINT 类型，则对应脚本中的 PointData[] 类型。变量为数组类型的在脚本中直接使用。
> - 变量名称需要保证唯一性，即多个变量不要使用同一个名称。

```csharp
// 获取整个输入数组数据
PointData[] point1 = in0;

// 分别获取输入数组的每个数据
PointData[] point2 = new PointData[in0.Length];
for (int i=0;i<in0.Length;i++)
{
        point2[i] = in0[i];
}
```

### 输出数据示例（推荐用法）

推荐您在脚本中使用变量名称方式设置数据输出，假设您要设置输出变量名称为 `out0` 的 POINT 类型数据输出，代码示例如下：

> **说明：**
> - 使用 SetIntValue、SetRoiboxValue 等接口为上一代输出数据设置方式，出于方案兼容考虑，保留了这些接口，但不再推荐使用。
> - 实际使用中，请根据您的变量名称和变量类型修改代码片段，命名规则为 `<变量类型>Data[]`（适用变量类型为非数组类型）。变量为数组类型的在脚本中直接使用。
> - 变量名称需要保证唯一性。

```csharp
// 输入直接赋值给输出
out0=in0;

// 自定义数据赋值给输出
PointData[] pt = new PointData[in0.Length];
for (int i=0;i<in0.Length;i++)
 {
        pt[i] = new PointData();
        pt[i].PointX=i;
        pt[i].PointY=i;
 }
out0 = pt;
```

## Init

- **接口原型**：`public void Init(){}`
- **功能描述**：初始化脚本。可在此方法中实现初始化相关操作。该方法在加载方案或预编译全局脚本时执行。

## Process

- **接口原型**：`public bool Process(){}`
- **功能描述**：定义**脚本**模块所在流程的执行逻辑。

## GlobalVariableModule.SetValue

- **接口原型**：`GlobalVariableModule.SetValue(string paramName,string paramValue)`
- **功能描述**：设置全局变量。
- **输入参数**：`paramName`：string 类型，全局变量名称
- **输出参数**：`paramValue`：string 类型，全局变量的值
- **返回值**：
  - `0`：调用成功
  - 非 `0` 返回值：调用失败

## GlobalVariableModule.GetValue

- **接口原型**：`object GlobalVariableModule.GetValue (string paramName)`
- **功能描述**：获取全局变量的值。
- **输入参数**：`paramName`：string 类型，变量名称
- **返回值**：如果调用成功，返回全局变量的值；如果调用异常，返回 `null`。

  > **说明：** 返回值为 object 类型，如需转成其他类型，请将 object 转成 string 再转至其他类型。

## CurrentProcess.GetModule

- **接口原型**：`CurrentProcess.GetModule(string paramModuleName).GetValue(string paramValueName)`
- **功能描述**：获取指定模块某个结果参数的值。
- **输入参数**：
  - `paramModuleName`：string 类型，模块名称。请从流程中查找模块名称。
  - `paramValueName`：string 类型，模块结果中某个参数的名称。请从 SDK 手册中查找参数名称。该 SDK 手册可从 VM 安装路径中获取：`..\Development\V4.x\Documentations`。

  > **说明：** 如果对应的模块在流程的 Group 中，传入 `paramModuleName` 的模块名称需附带 Group 名称，例如：`GetModule("Group1.图像源1")`。

- **返回值**：如果调用成功，返回结果参数的值；如果调用异常，返回 `null`。

## CurrentProcess.GetModule.SetValue

- **接口原型**：`CurrentProcess.GetModule(string paramModuleName).SetValue(string paramValueName，string paramValue)`
- **功能描述**：设置模块运行参数的值。
- **输入参数**：
  - `paramModuleName`：string 类型，模块名称。请从流程中查找模块名称。
  - `paramValueName`：string 类型，参数名称（Key）。**必须从对应模块的 `AlgorithmTab.xml` 中查得**，绝对禁止凭记忆或猜测。查询前置步骤：先从 `VisionMaster模块映射表.md`（位于 references/ 目录内）查模块所属工具箱英文名和模块英文名，确认后构造路径：`{VM根目录}\Applications\Module(sp)\x64\{工具箱英文名}\{模块英文名}\{模块英文名}AlgorithmTab.xml`。参数 Key 对应 `<Name>` 节点内容，参数范围见 `<MinValue>` / `<MaxValue>`。完整流程见 SKILL.md §6。
  - `paramValue`：string 类型，参数值（统一以字符串传入，VM 内部负责类型转换）

  > **说明：** 如果对应的模块在流程的 Group 中，传入 `paramModuleName` 的模块名称需附带 Group 名称，例如：`GetModule("Group1.图像源1")`。

  > **范围校验：** 设置值超出 XML 中 MinValue–MaxValue 范围时，必须告知用户参数超限，不得生成超限代码。

- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SendData

**表 2 具体方法**

| 接口原型 | 说明 |
| --- | --- |
| `GlobalCommunicateModule.GetDevice(int deviceID).GetAddress(int addressID).SendData(string data,DataType dataType)` | 指定某个 PLC/Modbus 设备发送 Int、float 或 string 类型数据。<br/>**输入参数：**<br/>- `deviceID`：int 类型，通信管理中设备的设备 ID。<br/>- `addressID`：int 类型，通信管理中设备的地址 ID。<br/>- `data`：string 类型，待发送的数据，如果发送多个，请用 `;` 隔开。<br/>- `dataType`：`DataType` 类型，待发送数据的类型，包含 int、float 和 string 三种。<br/>**返回值：** 0 调用成功；非 0 调用异常。 |
| `GlobalCommunicateModule.GetDevice(int deviceID).GetAddress(int addressID).SendData(byte[] bytedata,DataType.ByteType)` | 指定某个 PLC/Modbus 设备发送十六进制数据。<br/>**输入参数：**<br/>- `deviceID`：int 类型，通信管理中设备的设备 ID。<br/>- `addressID`：int 类型，通信管理中设备的地址 ID。<br/>- `bytedata`：`byte[]` 类型，待发送的十六进制数据。<br/>**返回值：** 0 调用成功；非 0 调用异常。 |
| `GlobalCommunicateModule.GetDevice(int deviceID).SendData(string data)` | 指定某个 TCP、UDP 或串口发送 string 类型的数据。<br/>**输入参数：**<br/>- `deviceID`：int 类型，通信管理中设备的设备 ID。<br/>- `data`：string 类型，待发送的数据。<br/>**返回值：** 0 调用成功；非 0 调用异常。 |
| `GlobalCommunicateModule.GetDevice(int deviceID).SendData(byte[] bytedata)` | 指定某个 TCP、UDP 或串口发送十六进制的数据。<br/>**输入参数：**<br/>- `deviceID`：int 类型，通信管理中设备的设备 ID。<br/>- `bytedata`：`byte[]` 类型，待发送的十六进制数据。<br/>**返回值：** 0 调用成功；非 0 调用异常。 |

（图 1 设备ID与地址ID示例：TCP 客户端和三菱 MC 的设备 ID 分别为 1 和 2，三菱 MC 的地址 ID 为 1。）

## GetModuleParam

- **接口原型**：`public int GetModuleParam(uint nModuleID, string paramKey, ref string paramValue)`
- **功能描述**：获取模块运行参数。
- **输入参数**：`nModuleID`：uint 类型，模块 ID。
- **输出参数**：
  - `paramKey`：string 类型，模块运行参数
  - `paramValue`：ref string 类型，模块运行参数的值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## BytesToPointset

- **接口原型**：`int BytesToPointset(byte[] inVariant, ref ContourPointData[] contourPointArray)`
- **功能描述**：将点集二进制数据转换为轮廓点数组数据。
- **输入参数**：`inVariant`：byte[] 类型，待转换的点集二进制数据。
- **输出参数**：`contourPointArray`：ContourPointData[] 类型，轮廓点数组数据。

  > **说明：** ContourPointData[] 为脚本模块定义的轮廓点数据结构。

- **返回值**：
  - 0：转换成功
  - 非 `0` 返回值：转换失败

## PointsetToBytes

- **接口原型**：`byte[] PointsetToBytes(ContourPointData[] contourPointArray)`
- **功能描述**：将轮廓点数组数据转换为点集二进制数据。
- **输入参数**：`contourPointArray`：ContourPointData[] 类型，待转换的轮廓点数组数据。

  > **说明：** ContourPointData[] 为脚本模块定义的轮廓点数据结构。

- **返回值**：
  - 非 `null` 返回值：转换成功，为点集二进制数据
  - `null`：转换失败

## GetIntValue

- **接口原型**：`int GetIntValue(string paramName, ref int paramValue)`
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：`paramValue`：int 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## GetFloatValue

- **接口原型**：`int GetFloatValue (string paramName, ref float paramValue)`
- **功能描述**：获取 float 类型变量的值。
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：`paramValue`：float 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## GetStringValue

- **接口原型**：`int GetStringValue (string paramName, ref string paramValue)`
- **功能描述**：获取 string 类型变量的值。
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：`paramValue`：string 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## GetBytesValue

- **接口原型**：`int GetBytesValue (string paramName,ref byte[] paramValue)`
- **功能描述**：获取 byte 数组类型变量的值。
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：`paramValue`：`byte[]` 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## GetIMAGEValue

- **接口原型**：`int GetIMAGEValue (string paramName, ref Image paramValue)`
- **功能描述**：获取图像数据。
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：`paramValue`：Image 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## GetRoiboxValue

- **接口原型**：`int GetRoiboxValue(string paramName, ref RoiboxData roiboxData)`
- **功能描述**：获取 ROI 的 BOX 数据（识别框等）。
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：`roiboxData`：`RoiboxData` 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## 数组类 Get/Set 的实际调用路径（重要）

下文列出的 `GetXxxArrayValue` / `SetXxxArrayValue` 等数组接口为官方公开文档的 3/4 参签名，属于 `ScriptMethods` 基类的"上一代接口"。

**但 VM 4.3.0 实际导出的 `UserProperty.cs` 中，partial property 内部调用的是 `Conceal.InternalMethods` 的 2 参重载**（如 `GetIntArrayValue(string, ref int[])` / `SetIntArrayValue(string, int[])`），通过 `(InternalObject as InternalMethods).XxxArrayValue(...)` 访问。

- 完整 2 参重载签名见 [InternalMethods.cs](./InternalMethods.cs)
- 普通脚本仍应使用直接赋值（`int[] arr = in0; out0 = arr;`），无需手写任何数组 Get/Set
- 仅当需要按字符串动态访问变量名时，才需要使用 `(InternalObject as InternalMethods).GetIntArrayValue("name", ref arr)` 等 2 参接口

---

## GetIntArrayValue

- **接口原型**：`int GetIntArrayValue(string paramName, ref int[] paramValue，out int arrayCount)`
- **功能描述**：获取 int 数组变量。
- **`InternalMethods` 2 参重载**：`int GetIntArrayValue(string paramName, ref int[] intData)` —— partial property 实际走此版本，见 [InternalMethods.cs](./InternalMethods.cs)
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：
  - `paramValue`：`int[]` 类型，变量值
  - `arrayCount`：int 类型，数组个数
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## GetFloatArrayValue

- **接口原型**：`int GetFloatArrayValue(string paramName, ref float[] paramValue，out int arrayCount)`
- **功能描述**：获取 float 型数组变量。
- **`InternalMethods` 2 参重载**：`int GetFloatArrayValue(string paramName, ref float[] floatData)` —— partial property 实际走此版本，见 [InternalMethods.cs](./InternalMethods.cs)
- **输入参数**：`paramName`：string 类型，变量值
- **输出参数**：
  - `paramValue`：`float[]` 类型，变量值
  - `arrayCount`：int 类型，数组个数
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## GetStringArrayValue

- **接口原型**：`int GetStringArrayValue(string paramName, ref string[] paramValue，out int arrrayCount)`
- **功能描述**：获取 string 类型数组变量的值。
- **`InternalMethods` 2 参重载**：`int GetStringArrayValue(string paramName, ref string[] stringData)` —— partial property 实际走此版本，见 [InternalMethods.cs](./InternalMethods.cs)
- **输入参数**：`paramName`：string 类型，变量名称
- **输出参数**：
  - `paramValue`：`string[]` 类型，变量值
  - `arrayCount`：int 类型，数组个数
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetIntValue

- **接口原型**：`int SetIntValue(string key, int value)`
- **功能描述**：设置 int 型变量的值。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：int 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetIntArrayValue

- **接口原型**：`SetIntArrayValue(string key, int[] valueArray, int index, int len)`
- **功能描述**：设置 int 数组变量的值。
- **`InternalMethods` 2 参重载**：`int SetIntArrayValue(string key, int[] valueArray)` —— partial property 实际走此版本，见 [InternalMethods.cs](./InternalMethods.cs)
- **输入参数**：
  - `key`：string 型，key 值。
  - `valueArray`：string[]，数组
  - `index`：int 型，数组的索引
  - `len`：int 型，数组的长度
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetFloatValue

- **接口原型**：`int SetFloatValue (string key, float value)`
- **功能描述**：设置 float 型变量值。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：float 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetFloatArrayValue

- **接口原型**：`SetFloatArrayValue(string key, float[] valueArray, int index, int len)`
- **功能描述**：设置 float 数组变量的值。
- **`InternalMethods` 2 参重载**：`int SetFloatArrayValue(string key, float[] valueArray)` —— partial property 实际走此版本，见 [InternalMethods.cs](./InternalMethods.cs)
- **输入参数**：
  - `key`：string 型，key 值。
  - `valueArray`：string[]，数组
  - `index`：int 型，数组的索引
  - `len`：int 型，数组的长度
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetStringValue

- **接口原型**：`int SetStringValue (string key, string value)`
- **功能描述**：设置 string 型变量的值。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：string 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetStringArrayValue

- **接口原型**：`SetStringArrayValue(string key, string[] valueArray, int index, int len)`
- **功能描述**：设置 string 数组变量的值。
- **`InternalMethods` 2 参重载**：`int SetStringArrayValue(string key, string[] valueArray)` —— partial property 实际走此版本，见 [InternalMethods.cs](./InternalMethods.cs)
- **输入参数**：
  - `key`：string 型，key 值。
  - `valueArray`：string[]，数组
  - `index`：int 型，数组的索引
  - `len`：int 型，数组的长度
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetBytesValue

- **接口原型**：`int SetBytesValue (string key, byte[] value)`
- **功能描述**：设置十六进制数据。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：`byte[]` 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetImageValue

- **接口原型**：`int SetImageValue (string key, ImageData value)`
- **功能描述**：设置图像数据。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：`ImageData` 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetStringValueByIndex

- **接口原型**：`int SetStringValueByIndex(string key, string value, int index, int total)`
- **功能描述**：按照索引设置 string 型数组内某个元素的值。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：string 类型，变量值
  - `index`：int 类型，数组的索引
  - `total`：int 类型，数组元素个数
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetIntValueByIndex

- **接口原型**：`int SetIntValueByIndex(string key, int value, int index, int total)`
- **功能描述**：按照索引设置 int 型数组内某个元素的值。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：int 类型，变量值
  - `index`：int 类型，数组的索引
  - `total`：int 类型,数组元素个数
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetFloatValueByIndex

- **接口原型**：`int SetFloatValueByIndex (string key, float value, int index, int total)`
- **功能描述**：按照索引设置 float 型数组内某个元素的值。
- **输入参数**：
  - `key`：string 类型，变量名称
  - `value`：float 类型，变量值
  - `index`：int 类型，数组的索引
  - `total`：int 类型，数组元素个数
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## SetRoiboxValue

- **接口原型**：`int SetRoiboxValue(string paramName, RoiboxData roiboxData)`
- **功能描述**：设置 ROI 的 BOX 数据（识别框等）。
- **输入参数**：
  - `paramName`：string 类型，变量名称
  - `roiboxData`：`RoiboxData` 类型，变量值
- **返回值**：
  - 0：调用成功
  - 非 `0` 返回值：调用异常

## ShowImage

- **接口原型**：`public void ShowImage(ImageData imageData)`
- **功能描述**：在渲染控件上显示图像。
- **输入参数**：`imageData`：ImageData 类型，图像数据对象
- **返回值**：无
- **备注**：该接口仅型号中带 6210 或 7120 的加密狗支持使用。

## DrawShape

- **接口原型**：`public void DrawShape(object shapeData, ShapeConfig shapeConfig = null)`
- **功能描述**：在渲染控件上绘制图形。
- **输入参数**：
  - `shapeData`：object 类型，图形数据对象
  - `shapeConfig`：ShapeConfig 类型，默认参数，图形属性对象
- **返回值**：无
- **备注**：该接口仅型号中带 6210 或 7120 的加密狗支持使用。

## ConsoleWrite

- **接口原型**：`void ConsoleWrite(string content)`
- **功能描述**：将异常信息打印至 DebugView 中。
- **输入参数**：`Content`：string 类型，待打印的内容
- **返回值**：无

## ShowMessageBox

- **接口原型**：`void ShowMessageBox(string msg)`
- **功能描述**：脚本运行异常时，通过弹窗提示。
- **输入参数**：`msg`：string 类型，弹窗内容
- **返回值**：无
