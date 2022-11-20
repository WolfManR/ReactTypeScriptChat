import Index from "./chat";
import {QueryClient, QueryClientProvider} from '@tanstack/react-query';
import SignInCheck from "./auth/sign-in-check";

const queryClient = new QueryClient();

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <div className="App">
                <SignInCheck>
                    <Index/>
                </SignInCheck>
            </div>
        </QueryClientProvider>
    )
}

export default App
