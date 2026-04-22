# SmartBuildingLighting

基于 WPF 的智能楼宇照明监控系统毕业设计实现。

## 技术栈
- .NET 6 WPF
- MVVM Toolkit
- SQLite + EF Core
- Quartz.NET
- HelixToolkit.Wpf
- LiveChartsCore

## 已实现模块
- 登录认证
- 仪表盘与能耗图表
- 2D 楼层平面图监控
- 3D 楼层监控
- 分组控制
- 情景模式
- 定时调度
- 操作日志
- 通信配置管理
- 模拟器 / Modbus TCP 双模式通信

## 默认账号
- 管理员: `admin / admin123`
- 普通用户: `user / user123`

## 构建
在当前非 Windows 环境下使用:

```bash
dotnet build SmartBuildingLighting.sln /p:EnableWindowsTargeting=true
```

## 测试
```bash
dotnet test tests/SmartBuildingLighting.Tests/SmartBuildingLighting.Tests.csproj
```

## 设计交付
- Figma 设计稿说明: [docs/设计交付说明.md](/Users/wxf/Desktop/周文旭毕设/SmartBuildingLighting/docs/设计交付说明.md)
- 用户操作手册: [docs/用户操作手册.md](/Users/wxf/Desktop/周文旭毕设/SmartBuildingLighting/docs/用户操作手册.md)
- 答辩演示稿大纲: [docs/答辩演示稿大纲.md](/Users/wxf/Desktop/周文旭毕设/SmartBuildingLighting/docs/答辩演示稿大纲.md)
- 本地可编辑答辩演示文稿: [毕业设计答辩演示稿 - output.pptx](/Users/wxf/Desktop/周文旭毕设/SmartBuildingLighting/docs/presentation-output/output.pptx)

## 当前说明
- 主项目按开题要求保持 `net6.0-windows`。
- 当前会话未提供可直接调用的 Canva 连接器，因此未在线生成 Canva 成品，只提供了可直接使用的大纲文案。
- Windows 真机 UI 运行验收仍需在 Windows 环境完成。
