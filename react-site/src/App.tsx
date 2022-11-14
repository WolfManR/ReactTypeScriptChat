import Index from "./chat";
import {QueryClient, QueryClientProvider} from '@tanstack/react-query';

const queryClient = new QueryClient();

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <div className="App">
                <Index/>
            </div>
        </QueryClientProvider>
    )
}

export default App
