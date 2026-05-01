import styles from "./StatsCard.module.css";

interface StatsCardProps {
  label: string;
  value: string;
  change: string;
}

export function StatsCard({ label, value, change }: StatsCardProps) {
  const isPositive = change.startsWith("+");

  return (
    <div className={styles.card}>
      <span className={styles.label}>{label}</span>
      <span className={styles.value}>{value}</span>
      <span className={isPositive ? styles.changeUp : styles.changeDown}>
        {change}
      </span>
    </div>
  );
}
