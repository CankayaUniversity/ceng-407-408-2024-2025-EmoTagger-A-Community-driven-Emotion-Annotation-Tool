<template>
  <b-card no-body>
    <b-card-header>
      <div class="header-title">
        <h4 class="card-title">Friends</h4>
      </div>
    </b-card-header>
    <b-card-body>
      <ul class="list-inline m-0 p-0">
        <li v-for="(item, index) in list" :key="index" class="d-flex mb-4 align-items-center">
          <img :src="item.image" alt="story-img" class="rounded-pill avatar-40" loading="lazy" />
          <div class="ms-3 flex-grow-1">
            <h6>{{ item.name }}</h6>
            <p class="mb-0">{{ item.designation }}</p>
          </div>
          <div class="dropdown">
            <span id="dropdownMenuButton9" class="dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false" role="button"> </span>
            <div class="dropdown-menu dropdown-menu-end" aria-labelledby="dropdownMenuButton9">
              <a class="dropdown-item" href="javascript:void(0);">Unfollow</a>
              <a class="dropdown-item" href="javascript:void(0);">Unfriend</a>
              <a class="dropdown-item" href="javascript:void(0);">block</a>
            </div>
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
    name: { type: String, required: true },
    designation: { type: String, required: true }
  }
}
</script>
