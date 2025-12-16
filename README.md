# SuperAutoBackup

> 启动软件时自动备份整个程序文件夹到指定位置

~~还在担心你辛苦配置的ci总是被人删吗/快来试试我们的SAB!~~ 

> [!WARNING]
> - **这个插件还在处于超前开发阶段，不可用于生产环境！**
> 
> - **This plugin is still in the early development stage and should not be used in production environments!**

> [!CAUTION]
> - **在使用本插件前请检查您的ClassIsland软件本体所在目录为单独文件夹。**
> 
> - **不建议在ClassIsland软件本体所在目录存放其他(过大)文件。**
>
>- **这个插件适用于 ClassIsland 1.x 版本**

---

## 实现的功能/未实现的大饼
 
**实现了：**
 1. 软件启动时自动备份整个程序文件夹到指定位置
 2. 设置备份数量上限
 3. 备份文件夹命名包含时间戳和版本号
 4. 支持手动触发备份
 
**大饼：**
 - 添加“行动”：备份ci
 - 检测到ci被删除时自动恢复最新备份
 - 优化设置界面
 - 启动时检查ClassIsland软件完整性

# 声明
 - 本插件为开源软件，遵循 LGPLv3 许可证。详情请参阅 [LICENSE](./LICENSE) 文件。
 - 开发者：[@Programmer-MrWang](https://github.com/Programmer-MrWang)/与Kimi AI共同编写。
 - 插件图标在[@MacroMeng](https://github.com/MacroMeng)设计的基础上改造而来
 - 欢迎issue/pr
 - 感谢 [ClassIsland](https://www.classisland.tech/) 团队提供的优秀平台。（）