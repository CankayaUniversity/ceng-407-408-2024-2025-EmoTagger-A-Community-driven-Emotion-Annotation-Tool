<template>
  <b-card no-body>
    <b-card-header>
      <div class="header-title">
        <h4 class="card-title">Stories</h4>
      </div>
    </b-card-header>
    <b-card-body>
      <ul class="list-inline m-0 p-0">
        <li v-for="(item, index) in list" :key="index" class="d-flex mb-4 align-items-center active">
          <img :src="item.image" class="rounded-pill avatar-70 p-1 border bg-soft-light img-fluid" loading="lazy" />
          <div class="ms-3">
            <h5>{{ item.design }}</h5>
            <p class="mb-0">{{ item.time }}</p>
          </div>
        </li>
      </ul>
    </b-card-body>
  </b-card>
</template>
<script>
export default {
  props: {
    list: {
      type: Array,
      default: () => []
    },
    image: { type: String, required: true },
    design: { type: String, required: true },
    time: { type: String, required: true }
  }
}
</script>
