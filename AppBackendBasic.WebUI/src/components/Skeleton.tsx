import React from 'react';

interface SkeletonProps {
    className?: string;
}

export const Skeleton: React.FC<SkeletonProps> = ({ className = '' }) => {
    return (
        <div className={`animate-pulse bg-gray-200 rounded ${className}`}></div>
    );
};

export const CardSkeleton: React.FC = () => {
    return (
        <div className="bg-white rounded-2xl p-6 border border-gray-100">
            <div className="flex items-center gap-4 mb-4">
                <Skeleton className="w-12 h-12 rounded-full" />
                <div className="flex-1">
                    <Skeleton className="h-4 w-32 mb-2" />
                    <Skeleton className="h-3 w-48" />
                </div>
            </div>
            <Skeleton className="h-20 w-full mb-4" />
            <div className="flex gap-2">
                <Skeleton className="h-8 w-20" />
                <Skeleton className="h-8 w-20" />
            </div>
        </div>
    );
};

export const TableRowSkeleton: React.FC = () => {
    return (
        <tr className="border-b border-gray-100">
            <td className="py-4 px-4">
                <Skeleton className="h-4 w-full" />
            </td>
            <td className="py-4 px-4">
                <Skeleton className="h-4 w-24" />
            </td>
            <td className="py-4 px-4">
                <Skeleton className="h-4 w-16" />
            </td>
            <td className="py-4 px-4">
                <Skeleton className="h-4 w-20" />
            </td>
        </tr>
    );
};

export const StatCardSkeleton: React.FC = () => {
    return (
        <div className="bg-white rounded-2xl p-6 border border-gray-100">
            <div className="flex items-center justify-between mb-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="w-10 h-10 rounded-xl" />
            </div>
            <Skeleton className="h-8 w-16 mb-2" />
            <Skeleton className="h-3 w-32" />
        </div>
    );
};

export const ProjectDashboardSkeleton: React.FC = () => {
    return (
        <div className="space-y-6">
            {/* Header Skeleton */}
            <div className="flex items-center justify-between">
                <div>
                    <Skeleton className="h-8 w-48 mb-2" />
                    <Skeleton className="h-4 w-64" />
                </div>
                <Skeleton className="h-10 w-32 rounded-full" />
            </div>

            {/* Stats Grid Skeleton */}
            <div className="grid md:grid-cols-4 gap-6">
                <StatCardSkeleton />
                <StatCardSkeleton />
                <StatCardSkeleton />
                <StatCardSkeleton />
            </div>

            {/* Content Cards Skeleton */}
            <div className="grid md:grid-cols-2 gap-6">
                <CardSkeleton />
                <CardSkeleton />
            </div>
        </div>
    );
};

export const ListSkeleton: React.FC<{ rows?: number }> = ({ rows = 5 }) => {
    return (
        <div className="space-y-4">
            {Array.from({ length: rows }).map((_, i) => (
                <div key={i} className="flex items-center gap-4 p-4 bg-white rounded-xl border border-gray-100">
                    <Skeleton className="w-12 h-12 rounded-full flex-shrink-0" />
                    <div className="flex-1">
                        <Skeleton className="h-4 w-3/4 mb-2" />
                        <Skeleton className="h-3 w-1/2" />
                    </div>
                    <Skeleton className="h-8 w-20 rounded-full" />
                </div>
            ))}
        </div>
    );
};
