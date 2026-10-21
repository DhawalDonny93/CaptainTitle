import { AlertCircle } from 'lucide-react';

const AiVisibility = () => {
    return (
        <div className="flex items-center justify-center h-64">
            <div className="text-center">
                <AlertCircle className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">AI Visibility Tracking</h3>
                <p className="text-gray-600">
                    This feature is currently under development.
                </p>
                <p className="text-sm text-gray-500 mt-2">
                    Please use the Visibility Dashboards page instead.
                </p>
            </div>
        </div>
    );
};

export default AiVisibility;
