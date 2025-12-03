import http from './axios';

interface LoginRequest {
  username: string;
  password: string;
}

interface LoginResponse {
  studentId: string;
  cookie: string;
  [key: string]: any;
}

const loginService = {
  /**
   * 学生登录
   * @param username 学号
   * @param password 密码
   * @returns 登录结果
   */
  login: async (username: string, password: string): Promise<LoginResponse> => {
    const data: LoginRequest = { username, password };
    return http.post('/Login', data);
  }
};

export default loginService;
