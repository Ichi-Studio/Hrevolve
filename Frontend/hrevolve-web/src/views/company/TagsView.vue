<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Edit, Delete, Search } from '@element-plus/icons-vue';
import { companyApi } from '@/api';
import type { Tag } from '@/types';

const { t } = useI18n();

// 状态
const loading = ref(false);
const tags = ref<Tag[]>([]);
const dialogVisible = ref(false);
const dialogTitle = ref('');
const form = ref<Partial<Tag>>({});
const saving = ref(false);
const searchKeyword = ref('');

// 预设颜色
const presetColors = [
  '#D4AF37', '#F4D03F', '#E74C3C', '#3498DB', '#2ECC71',
  '#9B59B6', '#1ABC9C', '#E67E22', '#34495E', '#95A5A6'
];

// 计算对比色（黑/白）
const getContrastColor = (hexcolor: string) => {
  if (!hexcolor) return '#fff';
  // 移除 #
  const hex = hexcolor.replace('#', '');
  // 转 RGB
  const r = parseInt(hex.substring(0, 2), 16);
  const g = parseInt(hex.substring(2, 4), 16);
  const b = parseInt(hex.substring(4, 6), 16);
  // 计算亮度 (YIQ)
  const yiq = ((r * 299) + (g * 587) + (b * 114)) / 1000;
  return (yiq >= 128) ? '#000000' : '#ffffff';
};

// 过滤后的标签
const filteredTags = computed(() => {
  if (!searchKeyword.value) return tags.value;
  const keyword = searchKeyword.value.toLowerCase();
  return tags.value.filter(t => 
    t.name.toLowerCase().includes(keyword) || 
    t.category?.toLowerCase().includes(keyword)
  );
});

// 按分类分组
const groupedTags = computed(() => {
  const groups: Record<string, Tag[]> = {};
  filteredTags.value.forEach(tag => {
    const category = tag.category || t('tags.uncategorized');
    if (!groups[category]) groups[category] = [];
    groups[category].push(tag);
  });
  return groups;
});

// 获取数据
const fetchData = async () => {
  loading.value = true;
  try {
    const res = await companyApi.getTags();
    tags.value = res.data;
  } catch { /* ignore */ } finally { loading.value = false; }
};

onMounted(() => fetchData());

// 新增标签
const handleAdd = () => {
  form.value = { color: presetColors[0], isActive: true };
  dialogTitle.value = t('tags.addTag');
  dialogVisible.value = true;
};

// 编辑标签
const handleEdit = (tag: Tag) => {
  form.value = { ...tag };
  dialogTitle.value = t('tags.editTag');
  dialogVisible.value = true;
};

// 删除标签
const handleDelete = async (tag: Tag) => {
  await ElMessageBox.confirm(t('tags.confirmDelete', { name: tag.name }), t('assistantExtra.tip'), { type: 'warning' });
  try {
    await companyApi.deleteTag(tag.id);
    ElMessage.success(t('common.success'));
    fetchData();
  } catch { /* ignore */ }
};

// 保存
const handleSave = async () => {
  if (!form.value.name) {
    ElMessage.warning(t('tags.enterName'));
    return;
  }
  saving.value = true;
  try {
    if (form.value.id) {
      await companyApi.updateTag(form.value.id, form.value);
    } else {
      await companyApi.createTag(form.value);
    }
    ElMessage.success(t('common.success'));
    dialogVisible.value = false;
    fetchData();
  } catch { /* ignore */ } finally { saving.value = false; }
};
</script>

<template>
  <div class="tags-view">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="card-title">{{ t('tags.management') }}</span>
          <div class="header-actions">
            <el-input
              v-model="searchKeyword"
              :placeholder="t('tags.searchPlaceholder')"
              :prefix-icon="Search"
              clearable
              class="search-input"
            />
            <el-button type="primary" :icon="Plus" @click="handleAdd">{{ t('tags.addTag') }}</el-button>
          </div>
        </div>
      </template>
      
      <div v-loading="loading" class="tags-content">
        <template v-if="Object.keys(groupedTags).length > 0">
          <div v-for="(categoryTags, category) in groupedTags" :key="category" class="tag-group">
            <div class="group-title">{{ category }}</div>
            <div class="tag-list">
              <div v-for="tag in categoryTags" :key="tag.id" class="tag-item" role="presentation">
                <el-tag
                  :color="tag.color"
                  :style="{ 
                    borderColor: tag.color,
                    color: getContrastColor(tag.color)
                  }"
                  effect="dark"
                  size="large"
                  class="custom-tag"
                >
                  {{ tag.name }}
                </el-tag>
                <div class="tag-actions">
                  <el-button link type="primary" size="small" @click="handleEdit(tag)">
                    <el-icon><Edit /></el-icon>
                  </el-button>
                  <el-button link type="danger" size="small" @click="handleDelete(tag)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </div>
              </div>
            </div>
          </div>
        </template>
        <el-empty v-else :description="t('tags.noData')" />
      </div>
    </el-card>
    
    <!-- 编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="450px">
      <el-form :model="form" label-width="80px">
        <el-form-item :label="t('tags.name')" required>
          <el-input v-model="form.name" :placeholder="t('tags.enterName')" />
        </el-form-item>
        <el-form-item :label="t('tags.category')">
          <el-input v-model="form.category" :placeholder="t('tags.categoryPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('tags.color')">
          <div class="color-picker">
            <div
              v-for="color in presetColors"
              :key="color"
              class="color-item"
              :class="{ active: form.color === color }"
              :style="{ backgroundColor: color }"
              @click="form.color = color"
            />
            <el-color-picker v-model="form.color" />
          </div>
        </el-form-item>
        <el-form-item :label="t('tags.description')">
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
.tags-view {
  :deep(.el-card) {
    border: none;
    background: transparent;
    
    .el-card__header {
      border-bottom: 1px solid rgba(255, 255, 255, 0.05);
      padding: 20px 0;
      margin: 0 20px;
    }
    
    .el-card__body {
      padding: 20px;
    }
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;

    .card-title { 
      font-size: 18px; 
      font-weight: 600; 
      color: #fff;
      display: flex;
      align-items: center;
      
      &::before {
        content: '';
        width: 4px;
        height: 18px;
        background: #D4AF37;
        margin-right: 12px;
        border-radius: 2px;
      }
    }

    .header-actions { 
      display: flex; 
      gap: 16px; 
      
      .search-input {
        width: 240px;
        transition: width 0.3s;
        
        &:focus-within {
          width: 300px;
        }
      }
    }
  }
  
  .tags-content {
    min-height: 200px;
  }
  
  .tag-group {
    margin-bottom: 32px;
    
    .group-title {
      font-size: 14px;
      font-weight: 600;
      color: rgba(255, 255, 255, 0.45);
      text-transform: uppercase;
      letter-spacing: 1px;
      margin-bottom: 16px;
      display: flex;
      align-items: center;
      
      &::after {
        content: '';
        flex: 1;
        height: 1px;
        background: rgba(255, 255, 255, 0.05);
        margin-left: 12px;
      }
    }
    
    .tag-list {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
    }
    
    .tag-item {
      position: relative;
      display: flex;
      align-items: center;
      padding: 6px;
      padding-right: 12px;
      cursor: default;
      
      &:hover {
        .tag-actions {
          opacity: 1;
          transform: translateX(0);
        }
      }

      .custom-tag {
        border: none;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        padding: 0 16px;
        height: 32px;
        font-weight: 500;
      }
      
      .tag-actions {
        opacity: 0;
        transform: translateX(-10px);
        transition: all 0.2s ease;
        margin-left: 8px;
        display: flex;
        gap: 4px;
      }
    }
  }
  
  .color-picker {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
    
    .color-item {
      width: 24px;
      height: 24px;
      border-radius: 4px;
      cursor: pointer;
      border: 2px solid transparent;
      transition: all 0.2s;
      
      &:hover { transform: scale(1.1); }
      &.active { border-color: #fff; box-shadow: 0 0 0 2px rgba(212, 175, 55, 0.5); }
    }
  }
}
</style>
