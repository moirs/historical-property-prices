import React, { type ReactNode } from 'react';

interface LayoutProps {
  children: ReactNode;
}

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  return (
    <div className="flex h-screen bg-gray-100">
      {children}
    </div>
  );
};

interface LeftPanelProps {
  children: ReactNode;
}

export const LeftPanel: React.FC<LeftPanelProps> = ({ children }) => {
  return (
    <div className="w-96 bg-white shadow-lg overflow-y-auto">
      {children}
    </div>
  );
};

interface RightPanelProps {
  children: ReactNode;
}

export const RightPanel: React.FC<RightPanelProps> = ({ children }) => {
  return (
    <div className="flex-1 overflow-hidden">
      {children}
    </div>
  );
};

interface SectionProps {
  title?: string;
  children: ReactNode;
  className?: string;
}

export const Section: React.FC<SectionProps> = ({
  title,
  children,
  className = '',
}) => {
  return (
    <div className={`p-6 border-b border-gray-200 ${className}`}>
      {title && <h2 className="text-lg font-semibold text-gray-900 mb-4">{title}</h2>}
      {children}
    </div>
  );
};
