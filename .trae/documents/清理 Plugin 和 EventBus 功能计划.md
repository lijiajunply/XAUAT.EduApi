亲爱的主人，我已经为您仔细检查了代码，这两个功能（Plugin 和 Event Bus）目前确实是闲置状态，为了给项目“瘦身”，我们把它们清理掉吧！✨

以下是为您准备的 **清理计划**：

### 🧹 第一步：清理闲置文件
我们将删除以下两个目录中的所有文件，彻底移除它们的定义：

1.  **Plugins (插件系统)**
    *   `XAUAT.EduApi/Plugins/IPlugin.cs`
    *   `XAUAT.EduApi/Plugins/PluginManager.cs`
    *   `XAUAT.EduApi/Plugins/PluginContext.cs`
2.  **Events (事件总线系统)**
    *   `XAUAT.EduApi/Events/IEventBus.cs`
    *   `XAUAT.EduApi/Events/EventBus.cs`
    *   `XAUAT.EduApi/Events/IEvent.cs`
    *   `XAUAT.EduApi/Events/IEventHandler.cs`
    *   `XAUAT.EduApi/Events/EventBase.cs`

### ✂️ 第二步：解除服务注册
我们需要修改 `ServiceCollectionExtensions.cs`，把这两个功能从服务容器的“登记表”中划掉：

*   删除 `AddPluginServices` 方法。
*   删除 `AddEventDrivenServices` 方法。
*   在 `AddAllServices` 主方法中，移除对上述两个方法的调用。
*   清理文件头部的 `using` 引用。

### 🧼 第三步：清理启动逻辑
最后，我们需要修改 `Program.cs`，移除在程序启动和停止时对插件的操作：

*   删除插件管理器的初始化代码（`pluginManager.Initialize()` 等）。
*   删除程序退出时停止插件的代码（`pluginManager.StopAllPlugins()` 等）。
*   清理文件头部的 `using` 引用。

---

这样做之后，您的项目代码会变得更加清爽简洁！如果您准备好了，请确认这个计划，我就立马开始动手啦！💪
