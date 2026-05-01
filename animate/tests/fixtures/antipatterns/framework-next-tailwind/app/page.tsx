import { FeatureCard } from "../components/FeatureCard";
import { PricingCard } from "../components/PricingCard";

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-between p-24">
      <div className="z-10 max-w-5xl w-full items-center justify-between font-mono text-sm lg:flex">
        <p className="fixed left-0 top-0 flex w-full justify-center border-b border-gray-300 bg-gradient-to-b from-zinc-200 pb-6 pt-8 backdrop-blur-2xl lg:static lg:w-auto lg:rounded-xl lg:border lg:bg-gray-200 lg:p-4">
          Get started by editing&nbsp;
          <code className="font-mono font-bold">app/page.tsx</code>
        </p>
      </div>

      <div className="mb-32 text-center lg:max-w-5xl lg:w-full lg:mb-0 lg:text-left">
        <h1 className="text-5xl font-bold bg-gradient-to-r from-purple-400 to-cyan-400 bg-clip-text text-transparent mb-8">
          Welcome to Our Platform
        </h1>
        <p className="text-gray-400 text-lg mb-12">
          The next generation of web development
        </p>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <FeatureCard
            title="Fast"
            description="Lightning-fast performance out of the box"
            icon="⚡"
          />
          <FeatureCard
            title="Scalable"
            description="Grows with your business needs"
            icon="📈"
          />
          <FeatureCard
            title="Secure"
            description="Enterprise-grade security built in"
            icon="🔒"
          />
        </div>

        <div className="mt-16">
          <h2 className="text-purple-500 text-3xl font-bold text-center mb-8">Pricing</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <PricingCard name="Starter" price="$9" features={["5 projects", "Basic support"]} />
            <PricingCard name="Pro" price="$29" features={["Unlimited projects", "Priority support"]} highlighted />
          </div>
        </div>
      </div>
    </main>
  );
}
