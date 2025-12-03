<template>
  <div class="admin-layout">
    <!-- 侧边栏 -->
    <aside class="sidebar">
      <div class="sidebar-header">
        <h2>管理控制面板</h2>
      </div>
      <nav class="sidebar-nav">
        <el-menu
          :default-active="activeMenu"
          class="el-menu-vertical"
          @select="handleMenuSelect"
        >
          <el-menu-item index="dashboard">
            <template #title>
              <el-icon><Menu /></el-icon>
              <span>仪表盘</span>
            </template>
          </el-menu-item>
          <el-menu-item index="health">
            <template #title>
              <el-icon><CircleCheck /></el-icon>
              <span>健康检查</span>
            </template>
          </el-menu-item>
          <el-menu-item index="metrics">
            <template #title>
              <el-icon><DataAnalysis /></el-icon>
              <span>Prometheus监控</span>
            </template>
          </el-menu-item>
          <el-menu-item index="courses">
            <template #title>
              <el-icon><Document /></el-icon>
              <span>课程管理</span>
            </template>
          </el-menu-item>
          <el-menu-item index="scores">
            <template #title>
              <el-icon><Reading /></el-icon>
              <span>成绩管理</span>
            </template>
          </el-menu-item>
          <el-menu-item index="login">
            <template #title>
              <el-icon><User /></el-icon>
              <span>登录测试</span>
            </template>
          </el-menu-item>
        </el-menu>
      </nav>
    </aside>

    <!-- 主内容区域 -->
    <main class="main-content">
      <header class="header">
        <div class="header-left">
          <h1>{{ pageTitle }}</h1>
        </div>
        <div class="header-right">
          <el-dropdown>
            <span class="user-info">
              <el-icon><User /></el-icon>
              管理员
              <el-icon class="el-icon--right"><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item>个人设置</el-dropdown-item>
                <el-dropdown-item>退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </header>

      <div class="content">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  Menu,
  CircleCheck,
  DataAnalysis,
  Document,
  Reading,
  User,
  ArrowDown
} from '@element-plus/icons-vue';

// 路由相关
const route = useRoute();
const router = useRouter();

// 响应式数据
const activeMenu = ref('');

// 根据当前路由设置激活的菜单
watch(() => route.path, (newPath) => {
  // 提取路由路径，转换为菜单index
  const pathMap: Record<string, string> = {
    '/': 'dashboard',
    '/health': 'health',
    '/metrics': 'metrics',
    '/courses': 'courses',
    '/scores': 'scores',
    '/login-test': 'login'
  };
  activeMenu.value = pathMap[newPath] || 'dashboard';
}, { immediate: true });

const pageTitle = computed(() => {
  const titles: Record<string, string> = {
    dashboard: '仪表盘',
    health: '健康检查',
    metrics: 'Prometheus监控',
    courses: '课程管理',
    scores: '成绩管理',
    login: '登录测试'
  };
  return titles[activeMenu.value] || '管理控制面板';
});

// 方法
const handleMenuSelect = (index: string) => {
  activeMenu.value = index;
  // 根据菜单index跳转对应的路由
  const routeMap: Record<string, string> = {
    dashboard: '/',
    health: '/health',
    metrics: '/metrics',
    courses: '/courses',
    scores: '/scores',
    login: '/login-test'
  };
  const path = routeMap[index];
  if (path) {
    router.push(path);
  }
};
</script>

<style scoped>
.admin-layout {
  display: flex;
  height: 100vh;
  background-color: #f0f2f5;
}

/* 侧边栏样式 */
.sidebar {
  width: 240px;
  background-color: #001529;
  color: white;
  display: flex;
  flex-direction: column;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.15);
}

.sidebar-header {
  padding: 20px;
  border-bottom: 1px solid #1890ff;
}

.sidebar-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.sidebar-nav {
  flex: 1;
  padding: 10px 0;
}

.el-menu-vertical {
  background-color: transparent;
  border-right: none;
}

.el-menu-item {
  color: rgba(255, 255, 255, 0.8);
  height: 50px;
  line-height: 50px;
  margin: 0 10px;
  border-radius: 4px;
}

.el-menu-item:hover {
  background-color: rgba(24, 144, 255, 0.2);
}

.el-menu-item.is-active {
  background-color: #1890ff;
  color: white;
}

/* 主内容区域 */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* 头部样式 */
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 20px;
  height: 60px;
  background-color: white;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.header-left h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: #303133;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-info {
  display: flex;
  align-items: center;
  cursor: pointer;
  padding: 8px 12px;
  border-radius: 4px;
  transition: background-color 0.3s;
}

.user-info:hover {
  background-color: #f5f7fa;
}

.user-info .el-icon {
  margin-right: 8px;
}

/* 内容区域 */
.content {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
}
</style>
