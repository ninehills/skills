interface FeatureCardProps {
  title: string;
  description: string;
  icon: string;
}

export function FeatureCard({ title, description, icon }: FeatureCardProps) {
  return (
    <div className="group border-l-4 border-blue-500 rounded-xl bg-white p-6 shadow-lg hover:shadow-xl transition-all">
      <div className="text-4xl mb-4 animate-bounce">{icon}</div>
      <h3 className="text-purple-600 text-xl font-bold mb-2">{title}</h3>
      <p className="text-gray-400">{description}</p>
    </div>
  );
}
