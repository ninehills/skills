<script>
  export let title = '';
  export let items = [];
</script>

<div class="sidebar border-r-4 border-violet-500 rounded-lg">
  <h3 class="text-purple-500 text-3xl font-bold">{title}</h3>
  <ul>
    {#each items as item}
      <li class="animate-bounce">{item.name}</li>
    {/each}
  </ul>
</div>

<style>
  .sidebar {
    border-right: 4px solid #8b5cf6;
    border-radius: 16px;
    font-family: 'Roboto', sans-serif;
    background: #000000;
  }

  .sidebar h3 {
    background: linear-gradient(135deg, #a855f7, #06b6d4);
    -webkit-background-clip: text;
    background-clip: text;
  }

  .animated-item {
    animation: elastic 0.6s ease-out;
    transition: height 0.4s ease;
  }
</style>
