<template>
  <div class="score-management">
    <el-card shadow="hover" class="score-header-card">
      <div class="card-header">
        <h3>成绩管理</h3>
        <el-button type="primary" size="small" @click="refreshScores">
          <el-icon><Refresh /></el-icon>
          刷新成绩
        </el-button>
      </div>
      <div class="search-bar">
        <el-input
          v-model="studentId"
          placeholder="输入学生ID"
          clearable
          style="width: 200px"
          prefix-icon="User"
        />
        <el-select
          v-model="selectedSemester"
          placeholder="选择学期"
          style="width: 180px"
          clearable
        >
          <el-option
            v-for="semester in semesters"
            :key="semester.value"
            :label="semester.name"
            :value="semester.value"
          />
        </el-select>
        <el-button type="primary" @click="getScores">
          <el-icon><Search /></el-icon>
          查询成绩
        </el-button>
        <el-button type="info" @click="getSemesters">
          <el-icon><Calendar /></el-icon>
          刷新学期
        </el-button>
      </div>
    </el-card>

    <el-card shadow="hover" class="score-content-card">
      <template #header>
        <div class="card-header">
          <h3>成绩列表</h3>
          <div class="header-info" v-if="scores && scores.length > 0">
            <span>共 {{ scores.length }} 门课程成绩</span>
          </div>
        </div>
      </template>
      
      <el-table
        v-if="scores && scores.length > 0"
        :data="scores"
        style="width: 100%"
        border
        stripe
      >
        <el-table-column prop="courseName" label="课程名称" min-width="200" />
        <el-table-column prop="score" label="成绩" width="100" />
        <el-table-column prop="semester" label="学期" width="150" />
        <el-table-column prop="credit" label="学分" width="80" />
        <el-table-column prop="courseType" label="课程类型" width="120" />
        <el-table-column prop="examTime" label="考试时间" width="180" />
        <el-table-column prop="teacher" label="教师" width="150" />
      </el-table>
      
      <div v-else-if="scores" class="empty-state">
        <el-empty description="未查询到成绩信息" />
      </div>
      
      <div v-else class="loading-state">
        <el-skeleton :rows="10" animated />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Refresh, Search, Calendar } from '@element-plus/icons-vue';
import { scoreService } from '../services';

// 响应式数据
const studentId = ref('');
const semesters = ref<any[]>([]);
const selectedSemester = ref('');
const scores = ref<any[]>([]);

// 获取学期列表
const getSemesters = async () => {
  try {
    const result = await scoreService.parseSemester();
    if (result.success && result.semesters) {
      semesters.value = result.semesters;
    }
  } catch (error) {
    console.error('获取学期列表失败:', error);
  }
};

// 获取成绩信息
const getScores = async () => {
  if (!studentId.value || !selectedSemester.value) {
    return;
  }
  try {
    const result = await scoreService.getScores(studentId.value, selectedSemester.value);
    scores.value = result;
  } catch (error) {
    console.error('获取成绩失败:', error);
  }
};

// 刷新成绩信息
const refreshScores = () => {
  getScores();
};

// 生命周期钩子
onMounted(() => {
  // 初始化获取学期列表
  getSemesters();
});
</script>

<style scoped>
.score-management {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.score-header-card {
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

.header-info {
  color: #909399;
  font-size: 14px;
}

.search-bar {
  display: flex;
  gap: 10px;
  padding: 15px 0;
  flex-wrap: wrap;
}

.score-content-card {
  border-radius: 8px;
}

.empty-state {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 40px 0;
}

.loading-state {
  padding: 20px 0;
}
</style>
