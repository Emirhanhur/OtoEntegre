import { defineComponent, h, onMounted, ref, resolveComponent } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

import { cilExternalLink } from '@coreui/icons'
import { CBadge, CSidebarNav, CNavItem, CNavGroup, CNavTitle } from '@coreui/vue'
import nav from '@/_nav.js'

import simplebar from 'simplebar-vue'
import 'simplebar-vue/dist/simplebar.min.css'

const normalizePath = (path) =>
  decodeURI(path)
    .replace(/#.*$/, '')
    .replace(/(index)?\.(html)$/, '')

const isActiveLink = (route, link) => {
  if (link === undefined) {
    return false
  }

  if (route.hash === link) {
    return true
  }

  const currentPath = normalizePath(route.path)
  const targetPath = normalizePath(link)

  return currentPath === targetPath
}

const isActiveItem = (route, item) => {
  if (isActiveLink(route, item.to)) {
    return true
  }

  if (item.items) {
    return item.items.some((child) => isActiveItem(route, child))
  }

  return false
}

const AppSidebarNav = defineComponent({
  name: 'AppSidebarNav',
  components: {
    CNavItem,
    CNavGroup,
    CNavTitle,
  },
  setup() {
    const route = useRoute()
    const firstRender = ref(true)

    onMounted(() => {
      firstRender.value = false
    })

    const renderItem = (item) => {
      // Hide admin-only items for non-admin users (client-side nav filter)
      if (item.meta && item.meta.requiresAdmin) {
        const rol = localStorage.getItem('rol')
        if (rol !== 'Admin') return null
      }

      if (item.items) {
  return h(
          CNavGroup,
          {
            as: 'div',
            compact: true,
            ...(firstRender.value && {
              visible: item.items.some((child) => isActiveItem(route, child)),
            }),
          },
          {
            togglerContent: () => [
              // render Material Icon instead of CoreUI CIcon
              h('span', { class: 'material-icons nav-icon' }, mapIconName(item.icon)),
              item.name,
            ],
            default: () => item.items.map((child) => renderItem(child)),
          },
        )
      }

      if (item.href) {
        return h(
          resolveComponent(item.component),
          {
            href: item.href,
            target: '_blank',
            rel: 'noopener noreferrer',
          },
          {
              default: () => [
              item.icon
                ? h('span', { class: 'material-icons nav-icon' }, mapIconName(item.icon))
                : h('span', { class: 'nav-icon' }, h('span', { class: 'nav-icon-bullet' })),
              item.name,
              item.external && h('span', { class: 'material-icons ms-2' }, mapIconName('cil-external-link')),
              item.badge &&
                h(
                  CBadge,
                  {
                    class: 'ms-auto',
                    color: item.badge.color,
                    size: 'sm',
                  },
                  {
                    default: () => item.badge.text,
                  },
                ),
            ],
          },
        )
      }

      return item.to
        ? h(
            RouterLink,
            {
              to: item.to,
              custom: true,
            },
            {
              default: (props) =>
                h(
                  resolveComponent(item.component),
                  {
                    active: props.isActive,
                    as: 'div',
                    href: props.href,
                    onClick: () => props.navigate(),
                  },
                  {
                    default: () => [
                      item.icon
                        ? h('span', { class: 'material-icons nav-icon' }, mapIconName(item.icon))
                        : h('span', { class: 'nav-icon' }, h('span', { class: 'nav-icon-bullet' })),
                      item.name,
                      item.badge &&
                        h(
                          CBadge,
                          {
                            class: 'ms-auto',
                            color: item.badge.color,
                            size: 'sm',
                          },
                          {
                            default: () => item.badge.text,
                          },
                        ),
                    ],
                  },
                ),
            },
          )
        : h(
            resolveComponent(item.component),
            {
              as: 'div',
            },
            {
              default: () => item.name,
            },
          )
    }

    // Map common CoreUI 'cil-' and 'cib-' icon names to Material icon names
    function mapIconName(name) {
      if (!name) return '';
      const map = {
        'cil-speedometer': 'speed',
        'cil-cart': 'shopping_cart',
        'cil-settings': 'settings',
        'cil-file-paper': 'article',
        'cil-list': 'format_list_bulleted',
        'cil-chart-line': 'trending_up',
        'cil-money': 'attach_money',
        'cil-external-link': 'open_in_new',
        'cil-options': 'more_vert',
        'cil-arrow-right': 'arrow_forward',
        'cil-people': 'people',
        'cil-user': 'person',
        'cil-user-follow': 'person_add',
        'cil-basket': 'shopping_basket',
        'cil-chart-pie': 'pie_chart',
        'cil-speedometer': 'speed',
        'cil-speech': 'chat_bubble',
        'cil-calendar': 'calendar_today',
        'cib-facebook': 'public',
        'cib-twitter': 'public',
        'cib-linkedin': 'public',
        'cil-moon': 'dark_mode',
        'cil-bell': 'notifications',
        'cil-arrow-top': 'arrow_upward',
        'cil-arrow-bottom': 'arrow_downward',
      }
      return map[name] || name.replace(/^cil-|^cib-/, '')
    }

    return () =>
      h(
        CSidebarNav,
        {
          as: simplebar,
        },
        {
          default: () => nav.map((item) => renderItem(item)),
        },
      )
  },
})

export { AppSidebarNav }
