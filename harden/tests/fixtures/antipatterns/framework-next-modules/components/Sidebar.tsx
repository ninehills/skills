import styles from "./Sidebar.module.css";

const navItems = [
  { label: "Overview", icon: "📊" },
  { label: "Analytics", icon: "📈" },
  { label: "Customers", icon: "👥" },
  { label: "Settings", icon: "⚙️" },
];

export function Sidebar() {
  return (
    <aside className={styles.sidebar}>
      <div className={styles.logo}>Dashboard</div>
      <nav className={styles.nav}>
        {navItems.map((item) => (
          <a key={item.label} href="#" className={styles.navItem}>
            <span>{item.icon}</span>
            <span>{item.label}</span>
          </a>
        ))}
      </nav>
    </aside>
  );
}
