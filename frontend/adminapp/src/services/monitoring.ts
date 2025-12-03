import http from './axios';

interface HealthCheckResult {
  status: string;
  checks: {
    name: string;
    status: string;
    description?: string;
    data?: any;
  }[];
  totalDuration: string;
  [key: string]: any;
}

const monitoringService = {
  /**
   * 获取健康检查状态
   * @returns 健康检查结果
   */
  getHealthStatus: async (): Promise<HealthCheckResult> => {
    return http.get('/health');
  },

  /**
   * 获取Prometheus指标
   * @returns Prometheus指标数据
   */
  getMetrics: async (): Promise<string> => {
    // 直接返回原始文本，因为Prometheus指标是文本格式
    const response = await fetch('/metrics');
    return response.text();
  }
};

export default monitoringService;
