<template>
  <div class="health-check">
    <el-card shadow="hover" class="health-status-card">
      <div class="card-header">
        <h3>系统健康状态</h3>
        <el-button type="primary" size="small" @click="refreshHealth">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
      </div>
      <div class="status-summary">
        <div class="status-item">
          <div class="status-indicator" :class="healthStatus === '健康' ? 'healthy' : 'unhealthy'"></div>
          <div class="status-text">
            <h2>{{ healthStatus }}</h2>
            <p>上次检查时间: {{ lastCheckedTime }}</p>
          </div>
        </div>
      </div>
    </el-card>

    <el-card shadow="hover" class="health-details-card">
      <template #header>
        <div class="card-header">
          <h3>健康检查详情</h3>
        </div>
      </template>
      <el-table :data="healthChecks" style="width: 100%">
        <el-table-column prop="name" label="检查项" width="180">
          <template #default="scope">
            <div class="check-name">{{ scope.row.name }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="120">
          <template #default="scope">
            <el-tag :type="scope.row.status === 'Healthy' ? 'success' : 'danger'">
              {{ scope.row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述"></el-table-column>
        <el-table-column prop="duration" label="耗时" width="100"></el-table-column>
        <el-table-column prop="data" label="详细数据" width="150">
          <template #default="scope">
            <el-button
              type="text"
              size="small"
              @click="showCheckDetails(scope.row)"
              v-if="scope.row.data"
            >
              查看详情
            </el-button>
            <span v-else>-</span>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 检查详情对话框 -->
    <el-dialog
      v-model="dialogVisible"
      title="检查项详情"
      width="60%"
    >
      <pre v-if="selectedCheck">{{ JSON.stringify(selectedCheck.data, null, 2) }}</pre>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Refresh } from '@element-plus/icons-vue';
import { monitoringService } from '../services';

// 响应式数据
const healthStatus = ref('未知');
const lastCheckedTime = ref('');
const healthChecks = ref<any[]>([]);
const dialogVisible = ref(false);
const selectedCheck = ref<any>(null);

// 格式化时间
const formatTime = () => {
  return new Date().toLocaleString();
};

// 刷新健康检查
const refreshHealth = async () => {
  try {
    const result = await monitoringService.getHealthStatus();
    healthStatus.value = result.status === 'Healthy' ? '健康' : '异常';
    lastCheckedTime.value = formatTime();
    
    // 处理健康检查数据
    if (result.checks) {
      healthChecks.value = result.checks.map((check: any) => ({
        name: check.name,
        status: check.status,
        description: check.description || '-',
        duration: check.duration || '-',
        data: check.data || null
      }));
    }
  } catch (error) {
    console.error('获取健康状态失败:', error);
    healthStatus.value = '异常';
    lastCheckedTime.value = formatTime();
  }
};

// 显示检查详情
const showCheckDetails = (check: any) => {
  selectedCheck.value = check;
  dialogVisible.value = true;
};

// 生命周期钩子
onMounted(() => {
  refreshHealth();
});
</script>

<style scoped>
.health-check {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.health-status-card {
  border-radius: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.status-summary {
  padding: 20px 0;
}

.status-item {
  display: flex;
  align-items: center;
  gap: 16px;
}

.status-indicator {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: #f56c6c;
  transition: background-color 0.3s;
}

.status-indicator.healthy {
  background-color: #67c23a;
}

.status-indicator.unhealthy {
  background-color: #f56c6c;
}

.status-text h2 {
  margin: 0;
  font-size: 28px;
  font-weight: 600;
  color: #303133;
}

.status-text p {
  margin: 4px 0 0 0;
  color: #909399;
  font-size: 14px;
}

.health-details-card {
  border-radius: 8px;
}

.check-name {
  font-weight: 500;
}
</style>
