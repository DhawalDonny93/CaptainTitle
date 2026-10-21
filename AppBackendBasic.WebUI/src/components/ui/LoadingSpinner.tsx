import { Loader2 } from 'lucide-react';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  text?: string;
}

export const LoadingSpinner = ({ size = 'md', className = '', text }: LoadingSpinnerProps) => {
  const sizeClasses = {
    sm: 'w-4 h-4',
    md: 'w-8 h-8',
    lg: 'w-12 h-12',
  };

  return (
    <div className={`flex flex-col items-center justify-center gap-3 ${className}`}>
      <Loader2 className={`${sizeClasses[size]} animate-spin text-gray-900`} />
      {text && <p className="text-sm text-gray-600">{text}</p>}
    </div>
  );
};

interface LoadingPageProps {
  text?: string;
  minHeight?: string;
}

export const LoadingPage = ({ text, minHeight = 'h-64' }: LoadingPageProps) => {
  return (
    <div className={`flex items-center justify-center ${minHeight}`}>
      <LoadingSpinner size="lg" text={text} />
    </div>
  );
};

interface LoadingButtonProps {
  children?: React.ReactNode;
}

export const LoadingButton = ({ children = 'Loading...' }: LoadingButtonProps) => {
  return (
    <div className="flex items-center justify-center gap-2">
      <Loader2 className="w-4 h-4 animate-spin" />
      <span>{children}</span>
    </div>
  );
};
