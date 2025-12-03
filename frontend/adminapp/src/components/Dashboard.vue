<template>
  <div class="dashboard">
    <!-- 状态卡片 -->
    <div class="status-cards">
      <el-card shadow="hover" class="status-card">
        <div class="card-content">
          <div class="card-icon health-icon">
            <el-icon><CircleCheck /></el-icon>
          </div>
          <div class="card-info">
            <h3>{{ healthStatus }}</h3>
            <p>系统健康状态</p>
          </div>
        </div>
      </el-card>

      <el-card shadow="hover" class="status-card">
        <div class="card-content">
          <div class="card-icon metrics-icon">
            <el-icon><DataAnalysis /></el-icon>
          </div>
          <div class="card-info">
            <h3>{{ metricsCount }}</h3>
            <p>监控指标数量</p>
          </div>
        </div>
      </el-card>

      <el-card shadow="hover" class="status-card">
        <div class="card-content">
          <div class="card-icon api-icon">
            <el-icon><Connection /></el-icon>
          </div>
          <div class="card-info">
            <h3>{{ apiCount }}</h3>
            <p>API接口数量</p>
          </div>
        </div>
      </el-card>

      <el-card shadow="hover" class="status-card">
        <div class="card-content">
          <div class="card-icon uptime-icon">
            <el-icon><Timer /></el-icon>
          </div>
          <div class="card-info">
            <h3>{{ uptime }}</h3>
            <p>系统运行时间</p>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 监控图表 -->
    <div class="charts-section">
      <el-card shadow="hover" class="chart-card">
        <template #header>
          <div class="card-header">
            <h3>系统资源使用情况</h3>
          </div>
        </template>
        <div id="resourceChart" ref="resourceChart" class="chart"></div>
      </el-card>

      <el-card shadow="hover" class="chart-card">
        <template #header>
          <div class="card-header">
            <h3>请求响应时间分布</h3>
          </div>
        </template>
        <div id="responseTimeChart" ref="responseTimeChart" class="chart"></div>
      </el-card>
    </div>

    <!-- 健康检查详情 -->
    <el-card shadow="hover" class="health-card">
      <template #header>
        <div class="card-header">
          <h3>健康检查详情</h3>
          <el-button type="primary" size="small" @click="refreshHealth">
            <el-icon><Refresh /></el-icon>
            刷新
          </el-button>
        </div>
      </template>
      <div class="health-checks">
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
        </el-table>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue';
import { CircleCheck, DataAnalysis, Connection, Timer, Refresh } from '@element-plus/icons-vue';
import * as echarts from 'echarts';
import { monitoringService } from '../services';

// 响应式数据
const healthStatus = ref('未知');
const metricsCount = ref(0);
const apiCount = ref(8); // 假设API接口数量
const uptime = ref('00:00:00');
const healthChecks = ref<any[]>([]);

// 图表引用
const resourceChart = ref<HTMLElement | null>(null);
const responseTimeChart = ref<HTMLElement | null>(null);
let resourceChartInstance: echarts.ECharts | null = null;
let responseTimeChartInstance: echarts.ECharts | null = null;

// 定时器
let uptimeTimer: number | null = null;
let startTime = Date.now();

// 格式化运行时间
const formatUptime = (ms: number) => {
  const seconds = Math.floor(ms / 1000);
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const remainingSeconds = seconds % 60;
  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
};

// 更新运行时间
const updateUptime = () => {
  const currentTime = Date.now();
  const elapsed = currentTime - startTime;
  uptime.value = formatUptime(elapsed);
};

// 初始化图表
const initCharts = () => {
  // 资源使用图表
  if (resourceChart.value) {
    resourceChartInstance = echarts.init(resourceChart.value);
    const resourceOption = {
      title: {
        text: 'CPU & Memory Usage',
        left: 'center',
        textStyle: {
          fontSize: 14
        }
      },
      tooltip: {
        trigger: 'axis',
        axisPointer: {
          type: 'cross',
          label: {
            backgroundColor: '#6a7985'
          }
        }
      },
      legend: {
        data: ['CPU Usage', 'Memory Usage'],
        bottom: 10
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '15%',
        top: '20%',
        containLabel: true
      },
      xAxis: [
        {
          type: 'category',
          boundaryGap: false,
          data: ['00:00', '02:00', '04:00', '06:00', '08:00', '10:00', '12:00']
        }
      ],
      yAxis: [
        {
          type: 'value',
          name: '使用率 (%)',
          max: 100
        }
      ],
      series: [
        {
          name: 'CPU Usage',
          type: 'line',
          stack: 'Total',
          smooth: true,
          lineStyle: {
            width: 3
          },
          areaStyle: {
            opacity: 0.3
          },
          data: [12, 28, 20, 40, 35, 55, 60]
        },
        {
          name: 'Memory Usage',
          type: 'line',
          stack: 'Total',
          smooth: true,
          lineStyle: {
            width: 3
          },
          areaStyle: {
            opacity: 0.3
          },
          data: [22, 38, 40, 50, 45, 65, 70]
        }
      ]
    };
    resourceChartInstance.setOption(resourceOption);
  }

  // 响应时间图表
  if (responseTimeChart.value) {
    responseTimeChartInstance = echarts.init(responseTimeChart.value);
    const responseTimeOption = {
      title: {
        text: 'API Response Time',
        left: 'center',
        textStyle: {
          fontSize: 14
        }
      },
      tooltip: {
        trigger: 'axis',
        axisPointer: {
          type: 'shadow'
        }
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '3%',
        containLabel: true
      },
      xAxis: [
        {
          type: 'category',
          data: ['登录', '课程', '成绩', '学期', '考试', '项目', '支付', '信息']
        }
      ],
      yAxis: [
        {
          type: 'value',
          name: '时间 (ms)'
        }
      ],
      series: [
        {
          name: '响应时间',
          type: 'bar',
          data: [120, 190, 300, 230, 290, 330, 310, 280]
        }
      ]
    };
    responseTimeChartInstance.setOption(responseTimeOption);
  }
};

// 刷新健康检查
const refreshHealth = async () => {
  try {
    const result = await monitoringService.getHealthStatus();
    healthStatus.value = result.status === 'Healthy' ? '健康' : '异常';
    
    // 处理健康检查数据
    if (result.checks) {
      healthChecks.value = result.checks.map((check: any) => ({
        name: check.name,
        status: check.status,
        description: check.description || '-',
        duration: check.duration || '-'
      }));
    }
  } catch (error) {
    console.error('获取健康状态失败:', error);
    healthStatus.value = '异常';
  }
};

// 获取指标数量
const refreshMetrics = async () => {
  try {
    const metrics = await monitoringService.getMetrics();
    // 简单计算指标数量（实际需要解析Prometheus格式）
    metricsCount.value = metrics.split('\n').filter(line => line && !line.startsWith('#')).length;
  } catch (error) {
    console.error('获取指标数量失败:', error);
    metricsCount.value = 0;
  }
};

// 监听窗口大小变化
const handleResize = () => {
  resourceChartInstance?.resize();
  responseTimeChartInstance?.resize();
};

// 生命周期钩子
onMounted(() => {
  // 初始化数据
  refreshHealth();
  refreshMetrics();
  
  // 初始化图表
  initCharts();
  
  // 启动运行时间定时器
  uptimeTimer = window.setInterval(updateUptime, 1000);
  
  // 添加窗口大小变化监听
  window.addEventListener('resize', handleResize);
});

onBeforeUnmount(() => {
  // 清除定时器
  if (uptimeTimer) {
    window.clearInterval(uptimeTimer);
  }
  
  // 销毁图表
  resourceChartInstance?.dispose();
  responseTimeChartInstance?.dispose();
  
  // 移除事件监听
  window.removeEventListener('resize', handleResize);
});
</script>

<style scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* 状态卡片 */
.status-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.status-card {
  border-radius: 8px;
  transition: transform 0.3s, box-shadow 0.3s;
}

.status-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.15) !important;
}

.card-content {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 0;
}

.card-icon {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  color: white;
}

.health-icon {
  background-color: #67c23a;
}

.metrics-icon {
  background-color: #409eff;
}

.api-icon {
  background-color: #e6a23c;
}

.uptime-icon {
  background-color: #f56c6c;
}

.card-info {
  flex: 1;
}

.card-info h3 {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.card-info p {
  margin: 0;
  color: #909399;
  font-size: 14px;
}

/* 图表区域 */
.charts-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 20px;
}

.chart-card {
  border-radius: 8px;
}

.chart {
  width: 100%;
  height: 300px;
}

/* 健康检查卡片 */
.health-card {
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

.health-checks {
  margin-top: 20px;
}

.check-name {
  font-weight: 500;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .charts-section {
    grid-template-columns: 1fr;
  }
  
  .status-cards {
    grid-template-columns: 1fr;
  }
}
</style>
