// OnboardingContentRenderer.tsx
// React Web component to render onboarding content with animations using Framer Motion

import { motion } from 'framer-motion';
import { useState, useEffect } from 'react';

// ============================================
// TYPE DEFINITIONS
// ============================================

interface ComparisonChartContent {
  type: 'comparison_chart';
  title: string;
  subtitle: string;
  data: {
    without: { label: string; value: string; color: string };
    with: { label: string; value: string; color: string };
  };
  footer: string;
  animation: string;
}

interface GoalChartContent {
  type: 'goal_chart';
  title: string;
  subtitle: string;
  chartData: {
    userPath: { label: string; color: string; points: number[] };
    traditionalPath: { label: string; color: string; points: number[] };
  };
  stats: {
    successRate: string;
    message: string;
  };
  animation: string;
}

interface ProfileSummaryContent {
  type: 'profile_summary';
  title: string;
  subtitle: string;
  level: { value: string; color: string; position: number };
  scale: { min: string; mid: string; max: string };
  warning: { title: string; message: string };
  metrics: Array<{
    icon: string;
    label: string;
    value: string;
    color?: string;
  }>;
  animation: string;
}

interface ProgressScreenContent {
  type: 'progress_screen';
  title: string;
  icon: string;
  steps: Array<{
    label: string;
    progress: number;
    status: string;
  }>;
  animation: string;
  autoAdvance: boolean;
  duration: number;
}

interface TestimonialsContent {
  type: 'testimonials';
  title: string;
  reviews: Array<{
    name: string;
    role: string;
    avatar: string;
    rating: number;
    text: string;
    expandable: boolean;
  }>;
  animation: string;
}

interface CompletionScreenContent {
  type: 'completion_screen';
  icon: string;
  iconStyle: {
    gradient: string[];
    animation: string;
  };
  badge: {
    icon: string;
    text: string;
    color: string;
  };
  title: string;
  subtitle?: string;
  message?: string;
  animation: string;
  autoAdvance: boolean;
  showProgress?: boolean;
  progressValue?: number;
}

interface ThankYouScreenContent {
  type: 'thank_you_screen';
  icon: string;
  iconStyle: {
    gradient: string[];
    animation: string;
  };
  title: string;
  subtitle: string;
  privacySection: {
    icon: string;
    title: string;
    message: string;
    backgroundColor: string;
  };
  animation: string;
  autoAdvance: boolean;
}

interface CelebrationScreenContent {
  type: 'celebration_screen';
  icon: string;
  iconStyle: {
    gradient: string[];
    animation: string;
    particles?: boolean;
  };
  title: string;
  subtitle: string;
  highlights: Array<{
    icon: string;
    text: string;
    color: string;
  }>;
  animation: string;
  autoAdvance: boolean;
}

type OnboardingContent =
  | ComparisonChartContent
  | GoalChartContent
  | ProfileSummaryContent
  | ProgressScreenContent
  | TestimonialsContent
  | CompletionScreenContent
  | ThankYouScreenContent
  | CelebrationScreenContent;

// ============================================
// MAIN RENDERER COMPONENT
// ============================================

interface OnboardingContentRendererProps {
  contentHtml: string | null;
}

export const OnboardingContentRenderer: React.FC<OnboardingContentRendererProps> = ({
  contentHtml,
}) => {
  if (!contentHtml) {
    return (
      <div className="flex items-center justify-center p-8">
        <p className="text-gray-500">No content available</p>
      </div>
    );
  }

  try {
    const content: OnboardingContent = JSON.parse(contentHtml);

    return (
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -20 }}
        transition={{ duration: 0.6 }}
        className="w-full"
      >
        {renderContent(content)}
      </motion.div>
    );
  } catch (error) {
    return (
      <div className="flex items-center justify-center p-8">
        <p className="text-gray-500">Unable to load content</p>
      </div>
    );
  }
};

const renderContent = (content: OnboardingContent) => {
  switch (content.type) {
    case 'comparison_chart':
      return <ComparisonChartView content={content} />;
    case 'goal_chart':
      return <GoalChartView content={content} />;
    case 'profile_summary':
      return <ProfileSummaryView content={content} />;
    case 'progress_screen':
      return <ProgressScreenView content={content} />;
    case 'testimonials':
      return <TestimonialsView content={content} />;
    case 'completion_screen':
      return <CompletionScreenView content={content} />;
    case 'thank_you_screen':
      return <ThankYouScreenView content={content} />;
    case 'celebration_screen':
      return <CelebrationScreenView content={content} />;
    default:
      return <p>Unknown content type</p>;
  }
};

// ============================================
// COMPARISON CHART VIEW
// ============================================

const ComparisonChartView: React.FC<{ content: ComparisonChartContent }> = ({
  content,
}) => {
  return (
    <div className="bg-white rounded-2xl p-8 shadow-lg max-w-2xl mx-auto">
      <h3 className="text-xl font-semibold text-center mb-2">{content.title}</h3>
      <p className="text-sm text-gray-600 text-center mb-8">{content.subtitle}</p>

      <div className="flex justify-around items-end h-64 mb-8">
        {/* Without PuffCount */}
        <div className="flex flex-col items-center">
          <motion.div
            initial={{ height: 0 }}
            animate={{ height: 80 }}
            transition={{ duration: 0.8, type: 'spring', stiffness: 100 }}
            className="w-24 rounded-xl flex items-center justify-center text-2xl font-semibold"
            style={{ backgroundColor: content.data.without.color, color: '#6B5DD3' }}
          >
            {content.data.without.value}
          </motion.div>
          <p className="text-sm text-gray-600 mt-3 text-center max-w-[100px]">
            {content.data.without.label}
          </p>
        </div>

        {/* With PuffCount */}
        <div className="flex flex-col items-center">
          <motion.div
            initial={{ height: 0 }}
            animate={{ height: 160 }}
            transition={{ duration: 0.8, delay: 0.2, type: 'spring', stiffness: 100 }}
            className="w-24 rounded-xl flex items-center justify-center text-4xl font-bold text-white"
            style={{ backgroundColor: content.data.with.color }}
          >
            {content.data.with.value}
          </motion.div>
          <p className="text-sm font-semibold mt-3 text-center max-w-[100px]" style={{ color: '#6B5DD3' }}>
            {content.data.with.label}
          </p>
        </div>
      </div>

      <p className="text-sm text-gray-600 text-center">{content.footer}</p>
    </div>
  );
};

// ============================================
// GOAL CHART VIEW
// ============================================

const GoalChartView: React.FC<{ content: GoalChartContent }> = ({ content }) => {
  return (
    <motion.div
      initial={{ scale: 0.9, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={{ duration: 0.5 }}
      className="bg-white rounded-2xl p-8 shadow-lg max-w-2xl mx-auto"
    >
      <h3 className="text-xl font-semibold text-center mb-2">{content.title}</h3>
      <p className="text-sm text-gray-600 text-center mb-8">{content.subtitle}</p>

      <div
        className="h-48 rounded-xl flex items-center justify-center mb-6"
        style={{
          background: `linear-gradient(to right, rgba(107, 93, 211, 0.1), rgba(255, 107, 107, 0.1))`,
        }}
      >
        <p className="text-gray-400">Chart Visualization</p>
      </div>

      <div className="flex justify-around mb-4">
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full" style={{ backgroundColor: content.chartData.userPath.color }} />
          <span className="text-xs text-gray-600">{content.chartData.userPath.label}</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full" style={{ backgroundColor: content.chartData.traditionalPath.color }} />
          <span className="text-xs text-gray-600">{content.chartData.traditionalPath.label}</span>
        </div>
      </div>

      <p className="text-sm text-center" style={{ color: '#6B5DD3' }}>
        <span className="font-bold">{content.stats.successRate}</span> {content.stats.message}
      </p>
    </motion.div>
  );
};

// ============================================
// PROFILE SUMMARY VIEW
// ============================================

const ProfileSummaryView: React.FC<{ content: ProfileSummaryContent }> = ({
  content,
}) => {
  return (
    <div className="bg-[#FFF9F5] rounded-2xl p-8 max-w-2xl mx-auto">
      <h3 className="text-2xl font-semibold text-center mb-2">{content.title}</h3>
      <p className="text-base text-gray-600 text-center mb-8">{content.subtitle}</p>

      {/* Level Indicator */}
      <div className="mb-8">
        <div
          className="h-2 rounded-full relative mb-2"
          style={{
            background: 'linear-gradient(to right, #4ADE80, #FCD34D, #F87171)',
          }}
        >
          <div
            className="absolute w-4 h-4 bg-white rounded-full border-3 -top-1"
            style={{
              left: `${content.level.position}%`,
              borderColor: content.level.color,
              borderWidth: '3px',
            }}
          />
        </div>
        <div className="flex justify-between text-xs">
          <span className="text-gray-600">{content.scale.min}</span>
          <span className="text-gray-600">{content.scale.mid}</span>
          <span className="font-semibold" style={{ color: content.level.color }}>
            {content.scale.max}
          </span>
        </div>
      </div>

      {/* Warning Box */}
      <div className="bg-red-100 rounded-xl p-4 mb-6 flex gap-3">
        <div
          className="w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 text-white font-bold"
          style={{ backgroundColor: content.level.color }}
        >
          !
        </div>
        <div>
          <h4 className="text-sm font-semibold mb-1">{content.warning.title}</h4>
          <p className="text-xs text-gray-600 leading-relaxed">{content.warning.message}</p>
        </div>
      </div>

      {/* Metrics Grid */}
      <div className="grid grid-cols-2 gap-3">
        {content.metrics.map((metric, index) => (
          <motion.div
            key={index}
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: index * 0.1, duration: 0.4 }}
            className="bg-white rounded-xl p-3 flex items-center gap-2"
          >
            <span className="text-2xl">{metric.icon}</span>
            <div>
              <p className="text-[10px] text-gray-400 mb-0.5">{metric.label}</p>
              <p
                className="text-sm font-semibold"
                style={{ color: metric.color || '#1F2937' }}
              >
                {metric.value}
              </p>
            </div>
          </motion.div>
        ))}
      </div>
    </div>
  );
};

// ============================================
// PROGRESS SCREEN VIEW
// ============================================

const ProgressScreenView: React.FC<{ content: ProgressScreenContent }> = ({
  content,
}) => {
  const [progress, setProgress] = useState(content.steps.map(() => 0));

  useEffect(() => {
    content.steps.forEach((step, index) => {
      setTimeout(() => {
        const interval = setInterval(() => {
          setProgress((prev) => {
            const newProgress = [...prev];
            if (newProgress[index] < step.progress) {
              newProgress[index] = Math.min(newProgress[index] + 2, step.progress);
            }
            return newProgress;
          });
        }, 20);

        setTimeout(() => clearInterval(interval), 1000);
      }, index * 500);
    });
  }, [content.steps]);

  return (
    <div className="flex flex-col items-center justify-center p-8 max-w-xl mx-auto">
      <motion.div
        animate={{ scale: [1, 1.1, 1] }}
        transition={{ duration: 1, repeat: Infinity }}
        className="text-8xl mb-8"
      >
        {content.icon}
      </motion.div>

      <h2 className="text-3xl font-bold text-center mb-12">{content.title}</h2>

      <div className="w-full space-y-6">
        {content.steps.map((step, index) => (
          <div key={index}>
            <div className="flex justify-between mb-2">
              <span className="text-base font-medium">{step.label}</span>
              <span
                className={`text-base font-semibold ${
                  step.status === 'completed' ? 'text-green-600' : 'text-gray-400'
                }`}
              >
                {progress[index]}%
              </span>
            </div>
            <div className="h-2 bg-gray-200 rounded-full overflow-hidden">
              <motion.div
                initial={{ width: 0 }}
                animate={{ width: `${progress[index]}%` }}
                transition={{ duration: 0.5 }}
                className="h-full rounded-full"
                style={{
                  background: 'linear-gradient(to right, #10B981, #059669)',
                }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

// ============================================
// TESTIMONIALS VIEW
// ============================================

const TestimonialsView: React.FC<{ content: TestimonialsContent }> = ({
  content,
}) => {
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null);

  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h2 className="text-3xl font-bold text-center mb-8">{content.title}</h2>

      <div className="space-y-5">
        {content.reviews.map((review, index) => (
          <motion.div
            key={index}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.1 }}
            className="bg-white rounded-2xl p-5 shadow-md"
          >
            <div className="flex items-center gap-3 mb-3">
              <img
                src={review.avatar}
                alt={review.name}
                className="w-12 h-12 rounded-full object-cover"
              />
              <div className="flex-1">
                <h4 className="font-semibold text-base">{review.name}</h4>
                <p className="text-sm text-gray-600">{review.role}</p>
              </div>
              <div className="flex gap-0.5">
                {Array.from({ length: review.rating }).map((_, i) => (
                  <span key={i} className="text-orange-400 text-sm">
                    ★
                  </span>
                ))}
              </div>
            </div>

            <p
              className={`text-sm text-gray-700 leading-relaxed mb-2 ${
                expandedIndex === index ? '' : 'line-clamp-2'
              }`}
            >
              {review.text}
            </p>

            {review.expandable && (
              <button
                onClick={() => setExpandedIndex(expandedIndex === index ? null : index)}
                className="text-sm font-semibold text-green-600 hover:text-green-700"
              >
                {expandedIndex === index ? 'Show Less' : 'Read More'}
              </button>
            )}
          </motion.div>
        ))}
      </div>
    </div>
  );
};

// ============================================
// COMPLETION SCREEN VIEW
// ============================================

const CompletionScreenView: React.FC<{ content: CompletionScreenContent }> = ({
  content,
}) => {
  return (
    <motion.div
      initial={{ scale: 0.8, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={{ duration: 0.5, type: 'spring' }}
      className="flex flex-col items-center justify-center p-8 max-w-xl mx-auto"
    >
      {/* Icon with Gradient */}
      <div
        className="w-48 h-48 rounded-full flex items-center justify-center mb-10 relative"
        style={{
          background: `linear-gradient(135deg, ${content.iconStyle.gradient[0]}, ${content.iconStyle.gradient[1]})`,
        }}
      >
        <motion.div
          animate={{ scale: [1, 1.1, 1] }}
          transition={{ duration: 2, repeat: Infinity }}
          className="text-7xl"
        >
          {content.icon}
        </motion.div>

        {/* Decorative dots */}
        {Array.from({ length: 12 }).map((_, i) => {
          const angle = (i * 30 * Math.PI) / 180;
          const x = Math.cos(angle) * 110;
          const y = Math.sin(angle) * 110;
          return (
            <div
              key={i}
              className="absolute w-1 h-1 bg-gray-300 rounded-full"
              style={{
                left: `calc(50% + ${x}px)`,
                top: `calc(50% + ${y}px)`,
              }}
            />
          );
        })}
      </div>

      {/* Badge */}
      <div
        className="flex items-center gap-2 px-4 py-2 rounded-full mb-4"
        style={{ backgroundColor: `${content.badge.color}15` }}
      >
        <span className="text-base">{content.badge.icon}</span>
        <span className="text-sm font-semibold" style={{ color: content.badge.color }}>
          {content.badge.text}
        </span>
      </div>

      {/* Title */}
      <h2 className="text-3xl font-bold text-center text-gray-900 mb-3">
        {content.title}
      </h2>

      {/* Subtitle */}
      {content.subtitle && (
        <p className="text-base text-gray-600 text-center">{content.subtitle}</p>
      )}
    </motion.div>
  );
};

// ============================================
// THANK YOU SCREEN VIEW
// ============================================

const ThankYouScreenView: React.FC<{ content: ThankYouScreenContent }> = ({
  content,
}) => {
  return (
    <div className="flex flex-col items-center justify-center p-8 max-w-xl mx-auto">
      {/* Icon with Gradient */}
      <div
        className="w-48 h-48 rounded-full flex items-center justify-center mb-10 relative"
        style={{
          background: `linear-gradient(135deg, ${content.iconStyle.gradient[0]}, ${content.iconStyle.gradient[1]})`,
        }}
      >
        <motion.div
          animate={{ rotate: [0, 15, 0] }}
          transition={{ duration: 1, repeat: Infinity }}
          className="text-7xl"
        >
          {content.icon}
        </motion.div>

        {/* Decorative dots */}
        {Array.from({ length: 12 }).map((_, i) => {
          const angle = (i * 30 * Math.PI) / 180;
          const x = Math.cos(angle) * 110;
          const y = Math.sin(angle) * 110;
          return (
            <div
              key={i}
              className="absolute w-1 h-1 bg-gray-300 rounded-full"
              style={{
                left: `calc(50% + ${x}px)`,
                top: `calc(50% + ${y}px)`,
              }}
            />
          );
        })}
      </div>

      {/* Title */}
      <h2 className="text-3xl font-bold text-center text-gray-900 mb-3">
        {content.title}
      </h2>

      {/* Subtitle */}
      <p className="text-base text-gray-600 text-center mb-10">{content.subtitle}</p>

      {/* Privacy Section */}
      <div
        className="w-full rounded-2xl p-6 text-center"
        style={{ backgroundColor: content.privacySection.backgroundColor }}
      >
        <div className="w-12 h-12 bg-white rounded-full flex items-center justify-center mx-auto mb-4 shadow-sm">
          <span className="text-2xl">{content.privacySection.icon}</span>
        </div>
        <h3 className="text-base font-semibold text-gray-900 mb-2">
          {content.privacySection.title}
        </h3>
        <p className="text-sm text-gray-600 leading-relaxed">
          {content.privacySection.message}
        </p>
      </div>
    </div>
  );
};

// ============================================
// CELEBRATION SCREEN VIEW
// ============================================

const CelebrationScreenView: React.FC<{ content: CelebrationScreenContent }> = ({
  content,
}) => {
  return (
    <div className="flex flex-col items-center justify-center p-8 max-w-xl mx-auto">
      {/* Icon with Gradient */}
      <div
        className="w-48 h-48 rounded-full flex items-center justify-center mb-10"
        style={{
          background: `linear-gradient(135deg, ${content.iconStyle.gradient[0]}, ${content.iconStyle.gradient[1]})`,
        }}
      >
        <motion.div
          animate={{ y: [-10, 0, -10] }}
          transition={{ duration: 1, repeat: Infinity }}
          className="text-7xl"
        >
          {content.icon}
        </motion.div>
      </div>

      {/* Title */}
      <h2 className="text-3xl font-bold text-center text-gray-900 mb-3">
        {content.title}
      </h2>

      {/* Subtitle */}
      <p className="text-base text-gray-600 text-center mb-10">{content.subtitle}</p>

      {/* Highlights */}
      <div className="w-full space-y-4">
        {content.highlights.map((highlight, index) => (
          <motion.div
            key={index}
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.5 + index * 0.15, duration: 0.4 }}
            className="flex items-center gap-3"
          >
            <div
              className="w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0"
              style={{ backgroundColor: `${highlight.color}20` }}
            >
              <span className="text-base font-semibold" style={{ color: highlight.color }}>
                {highlight.icon}
              </span>
            </div>
            <span className="text-base font-medium text-gray-900">{highlight.text}</span>
          </motion.div>
        ))}
      </div>
    </div>
  );
};

export default OnboardingContentRenderer;
