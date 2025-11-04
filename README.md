# GameDev-Lesson-Project
游戏设计与开发课程demo


## 🛠 开发环境

Unity 版本: 2022.3

## 📥 首次设置步骤

### 1. 克隆仓库
打开 GitHub Desktop

点击 File → Clone Repository

选择这个项目仓库

选择本地保存位置

点击 Clone

### Unity 环境准备
确保安装了相同版本的 Unity Editor

打开 Unity Hub

点击 Add 选择克隆下来的项目文件夹

### 3. 恢复依赖包
首次打开项目时，Unity 会自动：

导入必要的包

重新生成 Library 文件夹

下载依赖项

## 🔄 日常开发流程
### 开始工作前
拉取最新代码

打开 GitHub Desktop

点击 Fetch origin

如果有更新，点击 Pull origin

### 完成工作后
#### 提交更改

在 GitHub Desktop 左侧查看修改的文件

在右下角填写提交信息（必填）

点击 Commit to main

#### 推送更改

点击 Push origin 上传到远程仓库

## ⚠️ 重要注意事项
✅ 应该提交的文件
Assets/ 文件夹（游戏资源、脚本、场景）

ProjectSettings/（项目设置）

Packages/ 目录下的清单文件

❌ 不要提交的文件
Library/ 文件夹（自动生成）

Temp/ 文件夹

Build/ 文件夹

.csproj 文件

任何大型二进制文件（如 .dll, .exe）

## 🔒 文件锁定
如果使用 Unity Collaborate 或需要锁定场景：

在 Unity Editor 中右键场景文件

选择 Mark as Scene Edit

## 🐛 常见问题解决
### 项目打开异常
删除 Library 文件夹

重新用 Unity 打开项目

Unity 会自动重新生成必要文件

### 合并冲突
不要直接编辑他人的脚本文件

如有冲突，联系相关开发人员协商解决

场景文件冲突需要特别小心

### 包依赖问题
bash
#### 如果需要手动刷新包
在 Unity 中: Window > Package Manager > 点击刷新按钮
