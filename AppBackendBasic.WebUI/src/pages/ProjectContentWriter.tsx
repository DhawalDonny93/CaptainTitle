import { useParams } from 'react-router-dom';
import ContentWriter from './ContentWriter';

const ProjectContentWriter = () => {
    const { id } = useParams<{ id: string }>();
    const projectId = id ? parseInt(id, 10) : 0;

    if (!projectId || isNaN(projectId)) {
        return <div className="p-10 text-center">Invalid project ID</div>;
    }

    return <ContentWriter projectId={projectId} />;
};

export default ProjectContentWriter;
