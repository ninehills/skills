<template>
  <div class="card rounded-xl p-6 ring-1 ring-gray-200">
    <h2 class="text-lg font-semibold text-gray-900">{{ title }}</h2>
    <p class="text-gray-600 mt-2">{{ description }}</p>
    <button class="mt-4 px-4 py-2 bg-teal-600 text-white rounded-md">
      Get Started
    </button>
  </div>
</template>

<script setup>
defineProps({
  title: String,
  description: String,
});
</script>

<style scoped>
.card {
  font-family: 'Karla', system-ui, sans-serif;
  background: #fff;
}
</style>
