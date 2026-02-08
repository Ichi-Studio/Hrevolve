<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { companyApi } from '@/api';
import { Plus, OfficeBuilding, Location, Phone, Message, Link } from '@element-plus/icons-vue';
import type { Tenant } from '@/types';

const { t } = useI18n();
const loading = ref(false);
const saving = ref(false);
const form = ref<Partial<Tenant>>({});

// 行业选项（响应式）
const industryOptions = computed(() => [
  { label: t('company.industryIT'), value: 'IT' },
  { label: t('company.industryFinance'), value: 'Finance' },
  { label: t('company.industryManufacturing'), value: 'Manufacturing' },
  { label: t('company.industryRetail'), value: 'Retail' },
  { label: t('company.industryEducation'), value: 'Education' },
  { label: t('company.industryHealthcare'), value: 'Healthcare' },
  { label: t('company.industryOther'), value: 'Other' },
]);

// 规模选项（响应式）
const scaleOptions = computed(() => [
  { label: t('company.scale1_50'), value: '1-50' },
  { label: t('company.scale51_200'), value: '51-200' },
  { label: t('company.scale201_500'), value: '201-500' },
  { label: t('company.scale501_1000'), value: '501-1000' },
  { label: t('company.scale1000plus'), value: '1000+' },
]);

const fetchTenant = async () => {
  loading.value = true;
  try {
    const res = await companyApi.getTenant();
    form.value = res.data;
  } catch { /* ignore */ } finally { loading.value = false; }
};

const handleSave = async () => {
  saving.value = true;
  try {
    await companyApi.updateTenant(form.value);
    ElMessage.success(t('company.saveSuccess'));
  } catch { /* ignore */ } finally { saving.value = false; }
};

const handleLogoUpload = async (file: File) => {
  try {
    const res = await companyApi.uploadLogo(file);
    form.value.logo = res.data.url;
    ElMessage.success(t('company.logoUploadSuccess'));
  } catch { /* ignore */ }
  return false;
};

onMounted(() => fetchTenant());
</script>

<template>
  <div class="tenant-view">
    <el-card v-loading="loading" class="main-card">
      <template #header>
        <span class="card-title">{{ t('company.tenantInfo') }}</span>
      </template>
      
      <el-row :gutter="40">
        <el-col :xs="24" :lg="14">
          <el-form :model="form" label-width="120px">
            <el-form-item :label="t('company.companyLogo')">
              <el-upload
                class="logo-uploader"
                :show-file-list="false"
                :before-upload="handleLogoUpload"
                accept="image/*"
              >
                <img v-if="form.logo" :src="form.logo" class="logo-preview" />
                <el-icon v-else class="logo-placeholder"><Plus /></el-icon>
              </el-upload>
            </el-form-item>
            
            <el-form-item :label="t('company.companyCode')">
              <el-input v-model="form.code" disabled />
            </el-form-item>
            
            <el-form-item :label="t('company.companyName')" required>
              <el-input v-model="form.name" :placeholder="t('company.placeholder.companyName')" />
            </el-form-item>
            
            <el-form-item :label="t('company.industry')">
              <el-select v-model="form.industry" :placeholder="t('company.placeholder.industry')" style="width: 100%">
                <el-option v-for="opt in industryOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
            
            <el-form-item :label="t('company.companyScale')">
              <el-select v-model="form.scale" :placeholder="t('company.placeholder.scale')" style="width: 100%">
                <el-option v-for="opt in scaleOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
            
            <el-form-item :label="t('company.contactPhone')">
              <el-input v-model="form.phone" :placeholder="t('company.placeholder.phone')" />
            </el-form-item>
            
            <el-form-item :label="t('company.contactEmail')">
              <el-input v-model="form.email" :placeholder="t('company.placeholder.email')" />
            </el-form-item>
            
            <el-form-item :label="t('company.companyWebsite')">
              <el-input v-model="form.website" :placeholder="t('company.placeholder.website')" />
            </el-form-item>
            
            <el-form-item :label="t('company.companyAddress')">
              <el-input v-model="form.address" type="textarea" :rows="2" :placeholder="t('company.placeholder.address')" />
            </el-form-item>
            
            <el-form-item>
              <el-button type="primary" :loading="saving" @click="handleSave">{{ t('common.save') }}</el-button>
            </el-form-item>
          </el-form>
        </el-col>
        
        <el-col :xs="24" :lg="10" class="preview-col">
          <div class="preview-container">
            <div class="preview-card">
              <div class="preview-header">
                <img src="https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=abstract%20technology%20background%2C%20dark%20grey%20and%20gold%20lines%2C%20minimalist%2C%20geometric%2C%20premium%20business%20style&image_size=landscape_16_9" alt="Header" />
              </div>
              <div class="preview-content">
                <div class="preview-logo-wrapper">
                  <img v-if="form.logo" :src="form.logo" class="preview-logo" />
                  <div v-else class="preview-logo-placeholder">
                    <span class="company-initial">{{ form.name ? form.name.charAt(0).toUpperCase() : 'C' }}</span>
                  </div>
                </div>
                
                <h2 class="preview-company-name">{{ form.name || t('company.placeholder.companyName') }}</h2>
                <p class="preview-company-code">ID: {{ form.code || '------' }}</p>
                
                <div class="preview-tags">
                  <el-tag v-if="form.industry" effect="dark" type="warning" size="small" round>
                    {{ industryOptions.find(o => o.value === form.industry)?.label || form.industry }}
                  </el-tag>
                  <el-tag v-if="form.scale" effect="plain" type="info" size="small" round>
                    {{ scaleOptions.find(o => o.value === form.scale)?.label || form.scale }}
                  </el-tag>
                </div>
                
                <div class="preview-info-list">
                  <div class="info-item" v-if="form.phone">
                    <el-icon><Phone /></el-icon>
                    <span>{{ form.phone }}</span>
                  </div>
                  <div class="info-item" v-if="form.email">
                    <el-icon><Message /></el-icon>
                    <span>{{ form.email }}</span>
                  </div>
                  <div class="info-item" v-if="form.website">
                    <el-icon><Link /></el-icon>
                    <span>{{ form.website }}</span>
                  </div>
                  <div class="info-item" v-if="form.address">
                    <el-icon><Location /></el-icon>
                    <span>{{ form.address }}</span>
                  </div>
                </div>
              </div>
            </div>
            <div class="preview-hint">
              <el-icon><OfficeBuilding /></el-icon>
              <span>{{ t('company.previewCard', 'Company Card Preview') }}</span>
            </div>
          </div>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>



<style scoped lang="scss">
.tenant-view {
  .card-title {
    font-size: 16px;
    font-weight: 600;
  }
  
  .logo-uploader {
    :deep(.el-upload) {
      border: 1px dashed rgba(212, 175, 55, 0.3);
      border-radius: 8px;
      cursor: pointer;
      width: 120px;
      height: 120px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.3s;
      
      &:hover {
        border-color: #D4AF37;
      }
    }
  }
  
  .logo-preview {
    width: 120px;
    height: 120px;
    object-fit: contain;
    border-radius: 8px;
  }
  
  .logo-placeholder {
    font-size: 32px;
    color: rgba(255, 255, 255, 0.45);
  }

  // Preview Section Styles
  .preview-col {
    display: flex;
    justify-content: center;
    align-items: flex-start;
    padding-top: 20px;
  }

  .preview-container {
    width: 100%;
    max-width: 400px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
  }

  .preview-card {
    width: 100%;
    background: #252525;
    border-radius: 16px;
    overflow: hidden;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
    border: 1px solid rgba(255, 255, 255, 0.05);
    position: relative;
    transition: transform 0.3s ease;

    &:hover {
      transform: translateY(-5px);
      box-shadow: 0 15px 40px rgba(0, 0, 0, 0.4);
    }
  }

  .preview-header {
    height: 120px;
    width: 100%;
    background: #1a1a1a;
    position: relative;
    
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      opacity: 0.8;
    }
    
    &::after {
      content: '';
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      height: 60px;
      background: linear-gradient(to bottom, transparent, #252525);
    }
  }

  .preview-content {
    padding: 0 24px 32px;
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-top: -50px;
    position: relative;
    z-index: 1;
  }

  .preview-logo-wrapper {
    width: 100px;
    height: 100px;
    border-radius: 50%;
    background: #252525;
    padding: 4px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
    margin-bottom: 16px;
    display: flex;
    align-items: center;
    justify-content: center;
    
    .preview-logo {
      width: 100%;
      height: 100%;
      object-fit: cover;
      border-radius: 50%;
      background: #fff;
    }
    
    .preview-logo-placeholder {
      width: 100%;
      height: 100%;
      background: linear-gradient(135deg, #D4AF37, #F4D03F);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      
      .company-initial {
        font-size: 40px;
        font-weight: bold;
        color: #1a1a1a;
      }
    }
  }

  .preview-company-name {
    font-size: 20px;
    font-weight: 700;
    color: #fff;
    margin: 0 0 4px;
    text-align: center;
  }

  .preview-company-code {
    font-size: 13px;
    color: rgba(255, 255, 255, 0.5);
    margin: 0 0 16px;
    font-family: monospace;
  }

  .preview-tags {
    display: flex;
    gap: 8px;
    margin-bottom: 24px;
    flex-wrap: wrap;
    justify-content: center;
  }

  .preview-info-list {
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 12px;
    
    .info-item {
      display: flex;
      align-items: center;
      gap: 12px;
      color: rgba(255, 255, 255, 0.8);
      font-size: 14px;
      padding: 8px 12px;
      background: rgba(255, 255, 255, 0.03);
      border-radius: 8px;
      
      .el-icon {
        color: #D4AF37;
        font-size: 16px;
      }
      
      span {
        flex: 1;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
    }
  }

  .preview-hint {
    display: flex;
    align-items: center;
    gap: 8px;
    color: rgba(255, 255, 255, 0.4);
    font-size: 13px;
  }
}
</style>
