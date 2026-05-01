<script>
  export let title = '';
  export let items = [];
</script>

<div class="sidebar rounded-lg ring-1 ring-gray-200 p-4">
  <h3 class="text-lg font-semibold text-gray-900">{title}</h3>
  <ul>
    {#each items as item}
      <li class="py-2">{item.name}</li>
    {/each}
  </ul>
</div>

<style>
  .sidebar {
    font-family: 'Karla', system-ui, sans-serif;
    background: #fafafa;
  }
</style>
