<script setup>
const props = defineProps({
  href: String,
  tabContentClass: String,
})

const url = `https://coreui.io/vue/docs/${props.href}`
const addClass = props.tabContentClass
</script>

<template>
  <div class="example">
    <CNav variant="underline-border">
          <CNavItem>
        <CNavLink href="#" active>
          <span class="material-icons me-2">play_arrow</span>
          Önizleme
        </CNavLink>
      </CNavItem>
      <CNavItem>
        <CNavLink :href="url" target="_blank">
          <span class="material-icons me-2">code</span>
          Kod
        </CNavLink>
      </CNavItem>
    </CNav>
    <CTabContent :class="['rounded-bottom', addClass]">
      <CTabPane class="p-3 preview" visible>
        <slot></slot>
      </CTabPane>
    </CTabContent>
  </div>
</template>
