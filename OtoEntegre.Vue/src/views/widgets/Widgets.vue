<script setup>
import { getStyle } from '@coreui/utils'
import { CChart } from '@coreui/vue-chartjs'

import WidgetsStatsA from './WidgetsStatsTypeA.vue'
import WidgetsStatsD from './WidgetsStatsTypeD.vue'

const widgetStatsE = {
  labels: ['M', 'T', 'W', 'T', 'F', 'S', 'S', 'M', 'T', 'W', 'T', 'F', 'S', 'S', 'M'],
  optionsBar: {
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
    },
    scales: {
      x: {
        display: false,
      },
      y: {
        display: false,
      },
    },
  },
  optionsLine: {
    maintainAspectRatio: false,
    elements: {
      line: {
        tension: 0.4,
      },
      point: {
        radius: 0,
      },
    },
    plugins: {
      legend: {
        display: false,
      },
    },
    scales: {
      x: {
        display: false,
      },
      y: {
        display: false,
      },
    },
  },
}
</script>

<template>
  <CCard class="mb-4">
    <CCardHeader>
      <strong>Vue Widgets</strong>
    </CCardHeader>
    <CCardBody>
      <DocsExample href="components/widgets.html#widgetstatsa">
        <WidgetsStatsA />
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsb">
        <CRow :xs="{ gutter: 4 }">
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB :progress="{ color: 'success', value: 75 }">
              <template #text>Widget helper text</template>
              <template #title>Widget title</template>
              <template #value>89.9%</template>
            </CWidgetStatsB>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB
              :progress="{ color: 'info', value: 75 }"
              text="Widget helper text"
              title="Widget title"
              value="12.124"
            />
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB
              :progress="{ color: 'warning', value: 75 }"
              text="Widget helper text"
              title="Widget title"
              value="$98.111,00"
            />
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB
              :progress="{ color: 'primary', value: 75 }"
              text="Widget helper text"
              title="Widget title"
              value="2TB"
            />
          </CCol>
        </CRow>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsb">
        <CRow :xs="{ gutter: 4 }">
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB color="success" inverse :progress="{ value: 75 }">
              <template #text>Widget helper text</template>
              <template #title>Widget title</template>
              <template #value>89.9%</template>
            </CWidgetStatsB>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB
              color="info"
              inverse
              :progress="{ value: 75 }"
              text="Widget helper text"
              title="Widget title"
              value="12.124"
            />
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB
              color="warning"
              inverse
              :progress="{ value: 75 }"
              text="Widget helper text"
              title="Widget title"
              value="$98.111,00"
            />
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsB
              color="primary"
              inverse
              :progress="{ value: 75 }"
              text="Widget helper text"
              title="Widget title"
              value="2TB"
            />
          </CCol>
        </CRow>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatse">
        <CRow :xs="{ gutter: 4 }">
          <CCol :sm="4" :md="3" :xl="2">
            <CWidgetStatsE title="title" value="1,123">
              <CChart
                class="mx-auto"
                type="bar"
                style="height: 40px; width: 80px"
                :data="{
                  labels: widgetStatsE.labels,
                  datasets: [
                    {
                      backgroundColor: getStyle('--cui-danger'),
                      borderColor: 'transparent',
                      borderWidth: 1,
                      data: [41, 78, 51, 66, 74, 42, 89, 97, 87, 84, 78, 88, 67, 45, 47],
                    },
                  ],
                }"
                :options="widgetStatsE.optionsBar"
              />
            </CWidgetStatsE>
          </CCol>
          <CCol :sm="4" :md="3" :xl="2">
            <CWidgetStatsE title="title" value="1,123">
              <CChart
                class="mx-auto"
                type="bar"
                style="height: 40px; width: 80px"
                :data="{
                  labels: widgetStatsE.labels,
                  datasets: [
                    {
                      backgroundColor: getStyle('--cui-primary'),
                      borderColor: 'transparent',
                      borderWidth: 1,
                      data: [41, 78, 51, 66, 74, 42, 89, 97, 87, 84, 78, 88, 67, 45, 47],
                    },
                  ],
                }"
                :options="widgetStatsE.optionsBar"
              />
            </CWidgetStatsE>
          </CCol>
          <CCol :sm="4" :md="3" :xl="2">
            <CWidgetStatsE title="title" value="1,123">
              <CChart
                class="mx-auto"
                type="bar"
                style="height: 40px; width: 80px"
                :data="{
                  labels: widgetStatsE.labels,
                  datasets: [
                    {
                      backgroundColor: getStyle('--cui-warning'),
                      borderColor: 'transparent',
                      borderWidth: 1,
                      data: [41, 78, 51, 66, 74, 42, 89, 97, 87, 84, 78, 88, 67, 45, 47],
                    },
                  ],
                }"
                :options="widgetStatsE.optionsBar"
              />
            </CWidgetStatsE>
          </CCol>
          <CCol :sm="4" :md="3" :xl="2">
            <CWidgetStatsE title="title" value="1,123">
              <CChart
                class="mx-auto"
                type="line"
                style="height: 40px; width: 80px"
                :data="{
                  labels: widgetStatsE.labels,
                  datasets: [
                    {
                      backgroundColor: 'transparent',
                      borderColor: getStyle('--cui-success'),
                      borderWidth: 2,
                      data: [41, 78, 51, 66, 74, 42, 89, 97, 87, 84, 78, 88, 67, 45, 47],
                    },
                  ],
                }"
                :options="widgetStatsE.optionsLine"
              />
            </CWidgetStatsE>
          </CCol>
          <CCol :sm="4" :md="3" :xl="2">
            <CWidgetStatsE title="title" value="1,123">
              <CChart
                class="mx-auto"
                type="line"
                style="height: 40px; width: 80px"
                :data="{
                  labels: widgetStatsE.labels,
                  datasets: [
                    {
                      backgroundColor: 'transparent',
                      borderColor: getStyle('--cui-warning'),
                      borderWidth: 2,
                      data: [41, 78, 51, 66, 74, 42, 89, 97, 87, 84, 78, 88, 67, 45, 47],
                    },
                  ],
                }"
                :options="widgetStatsE.optionsLine"
              />
            </CWidgetStatsE>
          </CCol>
          <CCol :sm="4" :md="3" :xl="2">
            <CWidgetStatsE title="title" value="1,123">
              <CChart
                class="mx-auto"
                type="line"
                style="height: 40px; width: 80px"
                :data="{
                  labels: widgetStatsE.labels,
                  datasets: [
                    {
                      backgroundColor: 'transparent',
                      borderColor: getStyle('--cui-info'),
                      borderWidth: 2,
                      data: [41, 78, 51, 66, 74, 42, 89, 97, 87, 84, 78, 88, 67, 45, 47],
                    },
                  ],
                }"
                :options="widgetStatsE.optionsLine"
              />
            </CWidgetStatsE>
          </CCol>
        </CRow>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsf">
        <CRow :xs="{ gutter: 4 }">
          <CCol :sm="6" :xl="4" :xxl="3">
                <CWidgetStatsF color="primary" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">settings</span>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="info" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">person</span>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="warning" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">dark_mode</span>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="danger" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">notifications</span>
              </template>
            </CWidgetStatsF>
          </CCol>
        </CRow>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsf">
        <CRow :xs="{ gutter: 4 }">
          <CCol :sm="6" :xl="4" :xxl="3">
                <CWidgetStatsF color="primary" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">settings</span>
              </template>
              <template #footer>
                <CLink
                  class="fw-semibold font-xs text-body-secondary"
                  href="https://coreui.io/"
                  rel="noopener norefferer"
                  target="_blank"
                >
                  View more
                  <span class="material-icons ms-auto" style="font-size:16px">arrow_forward</span>
                </CLink>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="info" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">person</span>
              </template>
              <template #footer>
                <CLink
                  class="fw-semibold font-xs text-body-secondary"
                  href="https://coreui.io/"
                  rel="noopener norefferer"
                  target="_blank"
                >
                  View more
                  <span class="material-icons ms-auto" style="font-size:16px">arrow_forward</span>
                </CLink>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="warning" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">dark_mode</span>
              </template>
              <template #footer>
                <CLink
                  class="fw-semibold font-xs text-body-secondary"
                  href="https://coreui.io/"
                  rel="noopener norefferer"
                  target="_blank"
                >
                  View more
                  <span class="material-icons ms-auto" style="font-size:16px">arrow_forward</span>
                </CLink>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="danger" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">notifications</span>
              </template>
              <template #footer>
                <CLink
                  class="fw-semibold font-xs text-body-secondary"
                  href="https://coreui.io/"
                  rel="noopener norefferer"
                  target="_blank"
                >
                  View more
                  <span class="material-icons ms-auto" style="font-size:16px">arrow_forward</span>
                </CLink>
              </template>
            </CWidgetStatsF>
          </CCol>
        </CRow>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsf">
        <CRow :xs="{ gutter: 4 }">
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="primary" :padding="false" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">settings</span>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="info" :padding="false" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">person</span>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="warning" :padding="false" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">dark_mode</span>
              </template>
            </CWidgetStatsF>
          </CCol>
          <CCol :sm="6" :xl="4" :xxl="3">
            <CWidgetStatsF color="danger" :padding="false" title="Title" value="$1.999,50">
              <template #icon>
                <span class="material-icons" style="font-size:36px">notifications</span>
              </template>
            </CWidgetStatsF>
          </CCol>
        </CRow>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsd">
        <WidgetsStatsD />
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsc">
        <CCardGroup>
          <CWidgetStatsC :progress="{ color: 'info', value: 75 }" title="Visitors" value="87.500">
            <template #icon><span class="material-icons" style="font-size:36px">people</span></template>
          </CWidgetStatsC>
          <CWidgetStatsC
            :progress="{ color: 'success', value: 75 }"
            title="New Clients"
            value="385"
          >
            <template #icon><span class="material-icons" style="font-size:36px">person_add</span></template>
          </CWidgetStatsC>
          <CWidgetStatsC
            :progress="{ color: 'warning', value: 75 }"
            title="Products sold"
            value="1238"
          >
            <template #icon><span class="material-icons" style="font-size:36px">shopping_basket</span></template>
          </CWidgetStatsC>
          <CWidgetStatsC
            :progress="{ color: 'primary', value: 75 }"
            title="Returning Visitors"
            value="28%"
          >
            <template #icon><span class="material-icons" style="font-size:36px">pie_chart</span></template>
          </CWidgetStatsC>
          <CWidgetStatsC
            :progress="{ color: 'danger', value: 75 }"
            title="Avg. Time"
            value="5:34:11"
          >
            <template #icon><span class="material-icons" style="font-size:36px">speed</span></template>
          </CWidgetStatsC>
        </CCardGroup>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsc">
        <CRow :xs="{ gutter: 4 }">
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC :progress="{ color: 'info', value: 75 }" title="Visitors" value="87.500">
              <template #icon><span class="material-icons" style="font-size:36px">people</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              :progress="{ color: 'success', value: 75 }"
              title="New Clients"
              value="385"
            >
              <template #icon><span class="material-icons" style="font-size:36px">person_add</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              :progress="{ color: 'warning', value: 75 }"
              title="Products sold"
              value="1238"
            >
              <template #icon><span class="material-icons" style="font-size:36px">shopping_basket</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              :progress="{ color: 'primary', value: 75 }"
              title="Returning Visitors"
              value="28%"
            >
              <template #icon><span class="material-icons" style="font-size:36px">pie_chart</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              :progress="{ color: 'danger', value: 75 }"
              title="Avg. Time"
              value="5:34:11"
            >
              <template #icon><span class="material-icons" style="font-size:36px">speed</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC :progress="{ color: 'info', value: 75 }" title="Comments" value="972">
              <template #icon><span class="material-icons" style="font-size:36px">chat_bubble</span></template>
            </CWidgetStatsC>
          </CCol>
        </CRow>
      </DocsExample>
      <DocsExample href="components/widgets.html#widgetstatsc">
        <CRow :xs="{ gutter: 4 }">
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              color="info"
              inverse
              :progress="{ value: 75 }"
              title="Visitors"
              value="87.500"
            >
              <template #icon><span class="material-icons" style="font-size:36px">people</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              color="success"
              inverse
              :progress="{ value: 75 }"
              title="New Clients"
              value="385"
            >
              <template #icon><span class="material-icons" style="font-size:36px">person_add</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              color="warning"
              inverse
              :progress="{ value: 75 }"
              title="Products sold"
              value="1238"
            >
              <template #icon><span class="material-icons" style="font-size:36px">shopping_basket</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              color="primary"
              inverse
              :progress="{ value: 75 }"
              title="Returning Visitors"
              value="28%"
            >
              <template #icon><span class="material-icons" style="font-size:36px">pie_chart</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              color="danger"
              inverse
              :progress="{ value: 75 }"
              title="Avg. Time"
              value="5:34:11"
            >
              <template #icon><span class="material-icons" style="font-size:36px">speed</span></template>
            </CWidgetStatsC>
          </CCol>
          <CCol :xs="6" :lg="4" :xxl="2">
            <CWidgetStatsC
              color="info"
              inverse
              :progress="{ value: 75 }"
              title="Comments"
              value="972"
            >
              <template #icon><span class="material-icons" style="font-size:36px">chat_bubble</span></template>
            </CWidgetStatsC>
          </CCol>
        </CRow>
      </DocsExample>
    </CCardBody>
  </CCard>
</template>
