<template>
  <div class="course-management">
    <el-card shadow="hover" class="course-header-card">
      <div class="card-header">
        <h3>课程管理</h3>
        <el-button type="primary" size="small" @click="refreshCourses">
          <el-icon><Refresh /></el-icon>
          刷新课程
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
        <el-button type="primary" @click="getCourses">
          <el-icon><Search /></el-icon>
          查询课程
        </el-button>
      </div>
    </el-card>

    <el-card shadow="hover" class="course-content-card">
      <template #header>
        <div class="card-header">
          <h3>课程列表</h3>
          <div class="header-info" v-if="courses">
            <span>共 {{ courses.Data.length }} 门课程</span>
          </div>
        </div>
      </template>
      
      <el-table
        v-if="courses && courses.Data.length > 0"
        :data="courses.Data"
        style="width: 100%"
        border
        stripe
      >
        <el-table-column prop="courseName" label="课程名称" min-width="200" />
        <el-table-column prop="teacher" label="教师" width="150" />
        <el-table-column prop="time" label="上课时间" width="180" />
        <el-table-column prop="location" label="上课地点" width="150" />
        <el-table-column prop="credit" label="学分" width="80" />
        <el-table-column prop="examType" label="考试类型" width="100" />
        <el-table-column prop="capacity" label="容量" width="80" />
        <el-table-column prop="enrolled" label="已选" width="80" />
      </el-table>
      
      <div v-else-if="courses" class="empty-state">
        <el-empty description="未查询到课程信息" />
      </div>
      
      <div v-else class="loading-state">
        <el-skeleton :rows="10" animated />
      </div>
    </el-card>

    <el-card shadow="hover" v-if="courses" class="course-footer-card">
      <div class="footer-info">
        <p>数据最后更新时间: {{ courses.ExpirationTime }}</p>
        <p>查询学生ID: {{ studentId }}</p>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Refresh, Search } from '@element-plus/icons-vue';
import { courseService } from '../services';

// 响应式数据
const studentId = ref('');
const courses = ref<any>(null);

// 获取课程信息
const getCourses = async () => {
  if (!studentId.value) {
    return;
  }
  try {
    const result = await courseService.getCourses(studentId.value);
    courses.value = result;
  } catch (error) {
    console.error('获取课程失败:', error);
  }
};

// 刷新课程信息
const refreshCourses = () => {
  getCourses();
};
</script>

<style scoped>
.course-management {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.course-header-card {
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
}

.course-content-card {
  border-radius: 8px;
}

.course-footer-card {
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

.footer-info {
  color: #909399;
  font-size: 14px;
  display: flex;
  gap: 20px;
}

.footer-info p {
  margin: 0;
}
</style>
