import { Layout, LeftPanel, RightPanel, Section } from './components/Layout';
import { SearchForm } from './components/SearchForm';
import { ResultsPanel } from './components/ResultsPanel';
import { MapView } from './components/MapView';
import { useSearch } from './hooks/useSearch';
import { useState } from 'react';
import type { SearchFormState } from './types/index';

function App() {
  const { results, loading, error, executeSearch } = useSearch();
  const [lastPostcode, setLastPostcode] = useState<string>('');

  const handleSearch = (formState: SearchFormState) => {
    setLastPostcode(formState.postcode);
    executeSearch(formState);
  };

  return (
    <Layout>
      <LeftPanel>
        <Section title="Search Properties">
          <SearchForm onSearch={handleSearch} isLoading={loading} />
        </Section>
        <Section title="Results" className="flex-1 overflow-y-auto">
          <ResultsPanel
            results={results?.results || []}
            totalCount={results?.totalCount || 0}
            isLoading={loading}
            error={error}
          />
        </Section>
      </LeftPanel>
      <RightPanel>
        {results && results.results.length > 0 ? (
          <MapView properties={results.results} postcode={lastPostcode} />
        ) : (
          <div className="h-full flex items-center justify-center bg-gray-200">
            <p className="text-gray-600">
              Search for properties to view them on the map
            </p>
          </div>
        )}
      </RightPanel>
    </Layout>
  );
}

export default App;
