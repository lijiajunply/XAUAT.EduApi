import { createRouter, createWebHistory } from 'vue-router';
import Dashboard from '../components/Dashboard.vue';
import HealthCheck from '../components/HealthCheck.vue';
import Metrics from '../components/Metrics.vue';
import CourseManagement from '../components/CourseManagement.vue';
import ScoreManagement from '../components/ScoreManagement.vue';
import LoginTest from '../components/LoginTest.vue';

const routes = [
  {
    path: '/',
    name: 'Layout',
    component: () => import('../components/AdminLayout.vue'),
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: Dashboard
      },
      {
        path: 'health',
        name: 'HealthCheck',
        component: HealthCheck
      },
      {
        path: 'metrics',
        name: 'Metrics',
        component: Metrics
      },
      {
        path: 'courses',
        name: 'CourseManagement',
        component: CourseManagement
      },
      {
        path: 'scores',
        name: 'ScoreManagement',
        component: ScoreManagement
      },
      {
        path: 'login-test',
        name: 'LoginTest',
        component: LoginTest
      }
    ]
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

export default router;
