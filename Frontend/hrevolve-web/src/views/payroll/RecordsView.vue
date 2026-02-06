<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { payrollApi } from '@/api';
import type { PayrollRecord } from '@/types';

const { t } = useI18n();
const records = ref<PayrollRecord[]>([]);
const loading = ref(false);

const fetchRecords = async () => {
  loading.value = true;
  try { 
    const res = await payrollApi.getRecords({ page: 1, pageSize: 50 }); 
    records.value = res.data.items; 
  } catch { /* ignore */ } finally { loading.value = false; }
};

const formatMoney = (amount: unknown) => {
  const n = typeof amount === 'number' ? amount : Number(amount);
  return new Intl.NumberFormat('zh-CN', { style: 'currency', currency: 'CNY' }).format(Number.isFinite(n) ? n : 0);
};
const getStatusType = (s: string) => ({ Draft: 'info', Calculated: 'warning', Approved: '', Paid: 'success' }[s] || 'info');

// 状态标签映射 - 使用 computed 实现响应式翻译
const statusLabels = computed(() => ({
  Draft: t('payroll.statusDraft'),
  Calculated: t('payroll.statusCalculated'),
  Approved: t('payroll.statusApproved'),
  Paid: t('payroll.statusPaid'),
} as Record<string, string>));

onMounted(() => fetchRecords());
</script>

<template>
  <div class="records-view">
    <el-card>
      <template #header><span>{{ t('payrollAdmin.records') }}</span></template>
      <el-table :data="records" v-loading="loading" stripe style="width: 100%">
        <el-table-column prop="employeeName" :label="t('schedule.employee')" min-width="100" />
        <el-table-column prop="periodName" :label="t('payrollAdmin.period')" min-width="120" />
        <el-table-column prop="baseSalary" :label="t('payrollAdmin.baseSalary')" min-width="120"><template #default="{ row }">{{ formatMoney(row.baseSalary) }}</template></el-table-column>
        <el-table-column prop="netSalary" :label="t('payrollAdmin.netSalary')" min-width="120"><template #default="{ row }"><span style="color:#1890ff;font-weight:600">{{ formatMoney(row.netSalary) }}</span></template></el-table-column>
        <el-table-column prop="status" :label="t('common.status')" min-width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ statusLabels[row.status] || row.status }}</el-tag>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
