<template>
  <div class="example-container">
    <h2>服务连接示例</h2>
    
    <!-- 登录示例 -->
    <div class="section">
      <h3>登录功能</h3>
      <div class="form">
        <input 
          type="text" 
          v-model="loginForm.username" 
          placeholder="学号"
          class="input"
        />
        <input 
          type="password" 
          v-model="loginForm.password" 
          placeholder="密码"
          class="input"
        />
        <button @click="handleLogin" class="button">登录</button>
      </div>
      <div v-if="loginResult" class="result">
        <h4>登录结果:</h4>
        <pre>{{ loginResult }}</pre>
      </div>
    </div>

    <!-- 课程示例 -->
    <div class="section">
      <h3>课程查询</h3>
      <div class="form">
        <input 
          type="text" 
          v-model="courseStudentId" 
          placeholder="学生ID"
          class="input"
        />
        <button @click="handleGetCourses" class="button">获取课程</button>
      </div>
      <div v-if="courses" class="result">
        <h4>课程列表:</h4>
        <pre>{{ courses }}</pre>
      </div>
    </div>

    <!-- 成绩示例 -->
    <div class="section">
      <h3>成绩查询</h3>
      <div class="form">
        <input 
          type="text" 
          v-model="scoreStudentId" 
          placeholder="学生ID"
          class="input"
        />
        <input 
          type="text" 
          v-model="semester" 
          placeholder="学期代码，如2024-2025-2"
          class="input"
        />
        <button @click="handleGetScores" class="button">获取成绩</button>
      </div>
      <div v-if="scores" class="result">
        <h4>成绩列表:</h4>
        <pre>{{ scores }}</pre>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { loginService, courseService, scoreService } from '../services';

// 登录表单
const loginForm = ref({
  username: '',
  password: ''
});
const loginResult = ref<any>(null);

// 课程查询
const courseStudentId = ref('');
const courses = ref<any>(null);

// 成绩查询
const scoreStudentId = ref('');
const semester = ref('');
const scores = ref<any>(null);

// 登录处理
const handleLogin = async () => {
  try {
    const result = await loginService.login(loginForm.value.username, loginForm.value.password);
    loginResult.value = result;
  } catch (error) {
    console.error('登录失败:', error);
    loginResult.value = { error: '登录失败' };
  }
};

// 获取课程处理
const handleGetCourses = async () => {
  try {
    const result = await courseService.getCourses(courseStudentId.value);
    courses.value = result;
  } catch (error) {
    console.error('获取课程失败:', error);
    courses.value = { error: '获取课程失败' };
  }
};

// 获取成绩处理
const handleGetScores = async () => {
  try {
    const result = await scoreService.getScores(scoreStudentId.value, semester.value);
    scores.value = result;
  } catch (error) {
    console.error('获取成绩失败:', error);
    scores.value = { error: '获取成绩失败' };
  }
};
</script>

<style scoped>
.example-container {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
  font-family: Arial, sans-serif;
}

.section {
  margin-bottom: 30px;
  padding: 20px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
}

h2 {
  color: #333;
  text-align: center;
  margin-bottom: 30px;
}

h3 {
  color: #555;
  margin-bottom: 15px;
}

.form {
  display: flex;
  gap: 10px;
  margin-bottom: 15px;
  flex-wrap: wrap;
}

.input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  flex: 1;
  min-width: 200px;
}

.button {
  padding: 8px 16px;
  background-color: #42b983;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.3s;
}

.button:hover {
  background-color: #3aa876;
}

.result {
  background-color: #f5f5f5;
  padding: 15px;
  border-radius: 4px;
  overflow: auto;
}

pre {
  margin: 0;
  font-size: 12px;
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>
