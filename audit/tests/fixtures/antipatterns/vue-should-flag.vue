<template>
  <div class="card border-l-4 border-indigo-500 rounded-xl p-6">
    <h2 class="text-purple-600 text-2xl font-bold">{{ title }}</h2>
    <p class="text-gray-400">{{ description }}</p>
    <button class="animate-bounce mt-4 px-4 py-2 bg-indigo-500 text-white rounded">
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
  border-left: 4px solid #6366f1;
  border-radius: 12px;
  font-family: 'Inter', sans-serif;
}

.card h2 {
  background: linear-gradient(to right, #a855f7, #06b6d4);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}

.highlight {
  animation: bounce 1s infinite;
  transition: width 0.3s ease;
}
</style>
