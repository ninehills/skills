interface PricingCardProps {
  name: string;
  price: string;
  features: string[];
  highlighted?: boolean;
}

export function PricingCard({ name, price, features, highlighted }: PricingCardProps) {
  return (
    <div
      className={`rounded-2xl p-8 ${
        highlighted
          ? "bg-black border-t-4 border-violet-500 text-white"
          : "bg-white border border-gray-200"
      }`}
    >
      <h3 className="text-xl font-bold mb-2">{name}</h3>
      <div className="text-4xl font-bold mb-6 bg-gradient-to-r from-violet-500 to-fuchsia-500 bg-clip-text text-transparent">
        {price}
        <span className="text-sm text-gray-400">/mo</span>
      </div>
      <ul className="space-y-3">
        {features.map((feature) => (
          <li key={feature} className="flex items-center gap-2">
            <span className="text-violet-500">✓</span>
            {feature}
          </li>
        ))}
      </ul>
      <button className="mt-8 w-full py-3 rounded-lg bg-gradient-to-r from-purple-500 to-indigo-500 text-white font-semibold transition-all hover:scale-105">
        Get Started
      </button>
    </div>
  );
}
