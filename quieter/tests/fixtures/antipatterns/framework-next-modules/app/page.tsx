import { StatsCard } from "../components/StatsCard";
import { Sidebar } from "../components/Sidebar";
import styles from "./page.module.css";

export default function Dashboard() {
  return (
    <div className={styles.container}>
      <Sidebar />
      <main className={styles.main}>
        <h1 className={styles.title}>Dashboard</h1>
        <div className={styles.grid}>
          <StatsCard label="Revenue" value="$48,290" change="+12.5%" />
          <StatsCard label="Users" value="2,847" change="+8.1%" />
          <StatsCard label="Orders" value="1,024" change="-2.3%" />
        </div>
      </main>
    </div>
  );
}
