<template>
  <div class="login-test">
    <el-card shadow="hover" class="login-card">
      <template #header>
        <div class="card-header">
          <h3>登录测试</h3>
        </div>
      </template>
      <div class="login-form-container">
        <el-form
          ref="loginFormRef"
          :model="loginForm"
          :rules="rules"
          label-width="100px"
          class="login-form"
        >
          <el-form-item label="学号" prop="username">
            <el-input
              v-model="loginForm.username"
              placeholder="请输入学号"
              clearable
              prefix-icon="User"
            />
          </el-form-item>
          <el-form-item label="密码" prop="password">
            <el-input
              v-model="loginForm.password"
              type="password"
              placeholder="请输入密码"
              clearable
              show-password
              prefix-icon="Lock"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleLogin" :loading="isLoading" class="login-button">
              <el-icon><CircleCheck /></el-icon>
              登录
            </el-button>
            <el-button type="info" @click="resetForm" class="reset-button">
              <el-icon><Refresh /></el-icon>
              重置
            </el-button>
          </el-form-item>
        </el-form>
      </div>
    </el-card>

    <!-- 登录结果展示 -->
    <el-card shadow="hover" v-if="loginResult" class="result-card">
      <template #header>
        <div class="card-header">
          <h3>登录结果</h3>
        </div>
      </template>
      <div class="result-content">
        <div class="result-status" :class="loginResult.success ? 'success' : 'error'">
          <el-icon v-if="loginResult.success"><CircleCheck /></el-icon>
          <el-icon v-else><CircleClose /></el-icon>
          <span>{{ loginResult.success ? '登录成功' : '登录失败' }}</span>
        </div>
        <div class="result-details">
          <pre>{{ JSON.stringify(loginResult.data, null, 2) }}</pre>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { CircleCheck, Refresh, User, Lock, CircleClose } from '@element-plus/icons-vue';
import { loginService } from '../services';

// 响应式数据
const loginFormRef = ref();
const isLoading = ref(false);
const loginResult = ref<any>(null);

// 登录表单数据
const loginForm = reactive({
  username: '',
  password: ''
});

// 表单验证规则
const rules = {
  username: [
    { required: true, message: '请输入学号', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' }
  ]
};

// 登录处理
const handleLogin = async () => {
  if (!loginFormRef.value) return;
  
  try {
    await loginFormRef.value.validate();
    isLoading.value = true;
    
    const result = await loginService.login(loginForm.username, loginForm.password);
    loginResult.value = {
      success: true,
      data: result
    };
  } catch (error: any) {
    loginResult.value = {
      success: false,
      data: error.message || '登录失败'
    };
  } finally {
    isLoading.value = false;
  }
};

// 重置表单
const resetForm = () => {
  if (!loginFormRef.value) return;
  loginFormRef.value.resetFields();
  loginResult.value = null;
};
</script>

<style scoped>
.login-test {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.login-card {
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

.login-form-container {
  display: flex;
  justify-content: center;
  padding: 20px 0;
}

.login-form {
  width: 100%;
  max-width: 400px;
}

.login-button {
  width: 120px;
  margin-right: 10px;
}

.reset-button {
  width: 100px;
}

.result-card {
  border-radius: 8px;
}

.result-content {
  padding: 15px 0;
}

.result-status {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 15px;
  font-size: 16px;
  font-weight: 600;
}

.result-status.success {
  color: #67c23a;
}

.result-status.error {
  color: #f56c6c;
}

.result-details {
  background-color: #f5f7fa;
  padding: 15px;
  border-radius: 4px;
  overflow: auto;
  max-height: 400px;
}

.result-details pre {
  margin: 0;
  font-family: 'Courier New', Courier, monospace;
  font-size: 12px;
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>
