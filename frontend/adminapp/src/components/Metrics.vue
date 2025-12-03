<template>
  <div class="metrics">
    <el-card shadow="hover" class="metrics-header-card">
      <div class="card-header">
        <h3>Prometheus监控指标</h3>
        <div class="header-buttons">
          <el-button type="primary" size="small" @click="refreshMetrics">
            <el-icon><Refresh /></el-icon>
            刷新
          </el-button>
          <el-button size="small" @click="toggleMetricsView">
            <el-icon v-if="isRawView"><Document /></el-icon>
            <el-icon v-else><DataAnalysis /></el-icon>
            {{ isRawView ? '表格视图' : '原始视图' }}
          </el-button>
        </div>
      </div>
      <div class="metrics-summary">
        <div class="summary-item">
          <h4>{{ metricsCount }}</h4>
          <p>指标数量</p>
        </div>
        <div class="summary-item">
          <h4>{{ metricTypes.length }}</h4>
          <p>指标类型</p>
        </div>
        <div class="summary-item">
          <h4>{{ lastUpdatedTime }}</h4>
          <p>最后更新时间</p>
        </div>
      </div>
    </el-card>

    <!-- 原始指标视图 -->
    <el-card shadow="hover" v-if="isRawView" class="metrics-content-card">
      <template #header>
        <div class="card-header">
          <h3>原始指标数据</h3>
        </div>
      </template>
      <div class="raw-metrics">
        <pre>{{ rawMetrics }}</pre>
      </div>
    </el-card>

    <!-- 表格指标视图 -->
    <el-card shadow="hover" v-else class="metrics-content-card">
      <template #header>
        <div class="card-header">
          <h3>指标表格</h3>
          <el-input
            v-model="searchQuery"
            placeholder="搜索指标名称"
            clearable
            size="small"
            style="width: 200px"
          />
        </div>
      </template>
      <el-table
        :data="filteredMetrics"
        style="width: 100%"
        border
        stripe
        :height="500"
      >
        <el-table-column prop="name" label="指标名称" width="200" />
        <el-table-column prop="type" label="指标类型" width="120" />
        <el-table-column prop="value" label="指标值" width="150" />
        <el-table-column prop="labels" label="标签" min-width="300">
          <template #default="scope">
            <div class="labels">
              <el-tag
                v-for="(value, key) in scope.row.labels"
                :key="key"
                size="small"
                effect="plain"
                style="margin: 2px"
              >
                {{ key }}: {{ value }}
              </el-tag>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="help" label="指标描述" min-width="200" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { Refresh, Document, DataAnalysis } from '@element-plus/icons-vue';
import { monitoringService } from '../services';

// 响应式数据
const rawMetrics = ref('');
const parsedMetrics = ref<any[]>([]);
const metricsCount = ref(0);
const metricTypes = ref<string[]>([]);
const lastUpdatedTime = ref('');
const isRawView = ref(false);
const searchQuery = ref('');

// 格式化时间
const formatTime = () => {
  return new Date().toLocaleString();
};

// 解析Prometheus指标
const parseMetrics = (metricsText: string) => {
  const metrics: any[] = [];
  const lines = metricsText.split('\n');
  let currentHelp = '';
  let currentType = '';
  let currentName = '';

  lines.forEach(line => {
    line = line.trim();
    if (!line) return;

    // 解析帮助信息
    if (line.startsWith('# HELP')) {
      const match = line.match(/^# HELP (\w+) (.+)$/);
      if (match) {
        currentName = match[1] || '';
        currentHelp = match[2] || '';
      }
    }
    // 解析指标类型
    else if (line.startsWith('# TYPE')) {
      const match = line.match(/^# TYPE (\w+) (\w+)$/);
      if (match) {
        currentName = match[1] || '';
        currentType = match[2] || '';
      }
    }
    // 解析指标数据
    else if (!line.startsWith('#')) {
      const match = line.match(/^(\w+)({(.*)})? (.+)$/);
      if (match) {
        const name = match[1] || '';
        const labelsStr = match[3] || '';
        const value = match[4] || '';

        // 解析标签
        const labels: Record<string, string> = {};
        if (labelsStr) {
          const labelPairs = labelsStr.split(',');
          labelPairs.forEach(pair => {
            const [key, val] = pair.split('=');
            if (key && val) {
              labels[key.trim()] = val.trim().replace(/^"|"$/g, '');
            }
          });
        }

        metrics.push({
          name,
          type: currentType,
          help: currentHelp,
          value,
          labels
        });
      }
    }
  });

  return metrics;
};

// 刷新指标
const refreshMetrics = async () => {
  try {
    const metrics = await monitoringService.getMetrics();
    rawMetrics.value = metrics;
    
    // 解析指标
    parsedMetrics.value = parseMetrics(metrics);
    metricsCount.value = parsedMetrics.value.length;
    
    // 获取所有唯一指标类型
    const types = Array.from(new Set(parsedMetrics.value.map(m => m.type)));
    metricTypes.value = types.filter(type => type);
    
    lastUpdatedTime.value = formatTime();
  } catch (error) {
    console.error('获取指标失败:', error);
    rawMetrics.value = '获取指标失败，请检查服务器连接';
  }
};

// 切换指标视图
const toggleMetricsView = () => {
  isRawView.value = !isRawView.value;
};

// 过滤指标
const filteredMetrics = computed(() => {
  if (!searchQuery.value) {
    return parsedMetrics.value;
  }
  return parsedMetrics.value.filter(metric => 
    metric.name.toLowerCase().includes(searchQuery.value.toLowerCase())
  );
});

// 生命周期钩子
onMounted(() => {
  refreshMetrics();
});
</script>

<style scoped>
.metrics {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.metrics-header-card {
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

.header-buttons {
  display: flex;
  gap: 10px;
}

.metrics-summary {
  display: flex;
  gap: 40px;
  padding: 20px 0;
}

.summary-item {
  text-align: center;
}

.summary-item h4 {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.summary-item p {
  margin: 0;
  color: #909399;
  font-size: 14px;
}

.metrics-content-card {
  border-radius: 8px;
}

.raw-metrics {
  background-color: #f5f7fa;
  padding: 15px;
  border-radius: 4px;
  overflow: auto;
  max-height: 600px;
}

.raw-metrics pre {
  margin: 0;
  font-family: 'Courier New', Courier, monospace;
  font-size: 12px;
  white-space: pre-wrap;
  word-wrap: break-word;
}

.labels {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
</style>
