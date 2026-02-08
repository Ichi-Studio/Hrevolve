<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Edit, Delete, OfficeBuilding, Operation, Connection } from '@element-plus/icons-vue';
import { companyApi } from '@/api';
import type { CostCenter } from '@/types';

const { t } = useI18n();

const loading = ref(false);
const treeData = ref<CostCenter[]>([]);
const dialogVisible = ref(false);
const dialogTitle = ref('');
const form = ref<Partial<CostCenter>>({});
const saving = ref(false);

const defaultProps = { children: 'children', label: 'name' };

const fetchData = async () => {
  loading.value = true;
  try {
    const res = await companyApi.getCostCenterTree();
    treeData.value = res.data;
  } catch { /* ignore */ } finally { loading.value = false; }
};

const handleAdd = (parent?: CostCenter) => {
  form.value = { parentId: parent?.id, isActive: true };
  dialogTitle.value = parent ? t('costCenters.addChild', { name: parent.name }) : t('costCenters.management');
  dialogVisible.value = true;
};

const handleEdit = (data: CostCenter) => {
  form.value = { ...data };
  dialogTitle.value = t('costCenters.edit');
  dialogVisible.value = true;
};

const handleDelete = async (data: CostCenter) => {
  await ElMessageBox.confirm(t('costCenters.confirmDelete', { name: data.name }), t('assistantExtra.tip'), { type: 'warning' });
  try {
    await companyApi.deleteCostCenter(data.id);
    ElMessage.success(t('common.success'));
    fetchData();
  } catch { /* ignore */ }
};

const handleSave = async () => {
  saving.value = true;
  try {
    if (form.value.id) {
      await companyApi.updateCostCenter(form.value.id, form.value);
    } else {
      await companyApi.createCostCenter(form.value);
    }
    ElMessage.success(t('common.success'));
    dialogVisible.value = false;
    fetchData();
  } catch { /* ignore */ } finally { saving.value = false; }
};

onMounted(() => fetchData());
</script>

<template>
  <div class="cost-centers-view">
    <el-card shadow="hover" class="structure-card">
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <span class="title">{{ t('costCenters.management') }}</span>
          </div>
          <el-button type="primary" :icon="Plus" size="small" @click="handleAdd()">{{ t('costCenters.addRoot') }}</el-button>
        </div>
      </template>
      
      <div class="tree-container">
        <el-tree
          v-loading="loading"
          :data="treeData"
          :props="defaultProps"
          default-expand-all
          node-key="id"
          :indent="32"
          class="custom-tree"
          :highlight-current="true"
        >
          <template #default="{ node, data }">
            <div class="custom-tree-node" :class="{ 'is-root': node.level === 1 }">
              <div class="node-content">
                <div class="icon-wrapper" :class="[`level-${node.level}`, { 'has-children': data.children?.length }]">
                  <el-icon>
                    <OfficeBuilding v-if="node.level === 1" />
                    <Operation v-else-if="data.children && data.children.length > 0" />
                    <Connection v-else />
                  </el-icon>
                </div>
                <span class="node-label">{{ node.label }}</span>
                <el-tag v-if="data.code" size="small" type="info" effect="light">{{ data.code }}</el-tag>
                <el-tag v-if="!data.isActive" size="small" type="danger" effect="light">{{ t('costCenters.disabled') }}</el-tag>
              </div>
              
              <div class="node-meta">
                <div class="budget-info" v-if="data.budget" :class="{ 'is-active': data.budget > 0 }">
                  <span class="currency">¥</span>
                  <span class="amount">{{ data.budget.toLocaleString() }}</span>
                </div>
                
                <div class="node-actions">
                  <el-button link type="primary" size="small" @click.stop="handleAdd(data)">
                    <el-icon><Plus /></el-icon>
                  </el-button>
                  <el-button link type="primary" size="small" @click.stop="handleEdit(data)">
                    <el-icon><Edit /></el-icon>
                  </el-button>
                  <el-button link type="danger" size="small" @click.stop="handleDelete(data)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </div>
              </div>
            </div>
          </template>
        </el-tree>
        <el-empty v-if="!loading && treeData.length === 0" :description="t('costCenters.noData')" />
      </div>
    </el-card>
    
    <!-- 编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
      <el-form :model="form" label-width="100px">
        <el-form-item :label="t('costCenters.code')" required>
          <el-input v-model="form.code" :placeholder="t('costCenters.codePlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('costCenters.name')" required>
          <el-input v-model="form.name" :placeholder="t('costCenters.namePlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('costCenters.budget')">
          <el-input-number v-model="form.budget" :min="0" :precision="2" style="width: 100%" />
        </el-form-item>
        <el-form-item :label="t('costCenters.description')">
          <el-input v-model="form.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item :label="t('common.status')">
          <el-switch v-model="form.isActive" :active-text="t('settings.enabled')" :inactive-text="t('settings.disabled')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">{{ t('common.save') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
// Gold theme variables matching MainLayout
$gold-primary: #D4AF37;
$gold-light: #F4D03F;

// Dark theme backgrounds
$bg-card: #1A1A1A;
$text-primary: #FFFFFF;
$text-secondary: rgba(255, 255, 255, 0.85);

// Define active styles mixin for reuse
@mixin active-node-styles {
  .node-content {
    .node-label {
      color: $gold-primary;
    }
    
    .icon-wrapper {
      background: rgba(212, 175, 55, 0.2);
      color: $gold-primary;
    }
  }
  
  .node-meta .budget-info {
    background: rgba(212, 175, 55, 0.1);
    color: $gold-primary;
    
    .currency, .amount {
        color: $gold-primary;
    }
  }
}

.cost-centers-view {
  .structure-card {
    border-radius: 12px;
    border: 1px solid var(--el-border-color-lighter);
    background-color: $bg-card;
    transition: all 0.3s ease;
    
    &:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
      border-color: rgba(212, 175, 55, 0.3);
    }
    
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0 4px;
      
      .title {
        font-weight: 600;
        font-size: 16px;
        color: $text-primary;
      }
    }
  }

  .tree-container {
    padding: 8px 0;
  }

  :deep(.el-tree) {
    background-color: transparent;
    color: $text-secondary;
    --el-tree-node-content-height: 50px;
    --el-tree-node-hover-bg-color: transparent;
    --el-tree-text-color: $text-secondary;
    
    --el-color-primary-light-9: rgba(212, 175, 55, 0.15);
  }

  :deep(.el-tree-node__content) {
    border-radius: 8px;
    margin-bottom: 8px;
    border: 1px solid transparent;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    position: relative;
    overflow: hidden;
    background-color: rgba(255, 255, 255, 0.02);
    
    // Shine effect element
    &::before {
      content: '';
      position: absolute;
      top: 0;
      left: -100%;
      width: 100%;
      height: 100%;
      background: linear-gradient(90deg, 
        transparent 0%, 
        rgba(212, 175, 55, 0.1) 50%, 
        transparent 100%
      );
      transition: left 0.5s;
      pointer-events: none;
    }
    
    &:hover {
      background: linear-gradient(90deg, rgba(212, 175, 55, 0.15) 0%, rgba(212, 175, 55, 0.05) 100%);
      border-color: rgba(212, 175, 55, 0.2);
      
      &::before {
        left: 100%;
      }
      
      .custom-tree-node {
        @include active-node-styles;
        
        .node-meta .node-actions {
          opacity: 1;
          transform: translateX(0);
        }
      }
    }
  }

  // Handle is-current state
  :deep(.el-tree-node.is-current > .el-tree-node__content) {
    background: linear-gradient(90deg, rgba(212, 175, 55, 0.2) 0%, rgba(212, 175, 55, 0.08) 100%) !important;
    border-color: rgba(212, 175, 55, 0.3) !important;
    
    .custom-tree-node {
      @include active-node-styles;
    }
  }

  // Tree Guide Lines
  :deep(.el-tree-node) {
    position: relative;
    
    &::before {
      content: "";
      position: absolute;
      top: 0;
      bottom: 0;
      left: -18px; 
      border-left: 1px solid rgba(255, 255, 255, 0.1);
      width: 1px;
    }

    &:first-child::before {
      top: 25px;
    }

    &::after {
      content: "";
      position: absolute;
      left: -18px;
      top: 25px;
      width: 18px;
      height: 1px;
      border-top: 1px solid rgba(255, 255, 255, 0.1);
    }

    &:focus > .el-tree-node__content {
        background-color: rgba(212, 175, 55, 0.05);
    }
  }
  
  :deep(> .el-tree-node) {
    &::before { display: none; }
    &::after { display: none; }
  }

  .custom-tree-node {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: space-between;
    font-size: 14px;
    padding-right: 12px;
    position: relative;
    z-index: 2;
    
    .node-content {
      display: flex;
      align-items: center;
      gap: 12px;
      
      .icon-wrapper {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(255, 255, 255, 0.05);
        color: $text-secondary;
        transition: all 0.2s;
        
        &.level-1 {
          background: rgba(212, 175, 55, 0.2);
          color: $gold-primary;
        }
      }
      
      .node-label {
        font-weight: 500;
        color: $text-primary;
        font-size: 14px;
        transition: color 0.3s;
      }
    }

    .node-meta {
      display: flex;
      align-items: center;
      gap: 12px;

      .budget-info {
        display: flex;
        align-items: center;
        gap: 2px;
        padding: 4px 10px;
        border-radius: 12px;
        background: rgba(255, 255, 255, 0.05);
        color: $text-secondary;
        font-size: 12px;
        transition: all 0.2s;
        
        &.is-active {
          color: $text-primary;
          font-weight: 500;
        }
        
        .currency {
          font-size: 10px;
          opacity: 0.8;
        }
        
        .amount {
          font-family: var(--el-font-family-monospace);
        }
      }
      
      .node-actions {
        opacity: 0;
        transform: translateX(10px);
        transition: all 0.2s ease;
        display: flex;
        align-items: center;
      }
    }
  }
}
</style>
