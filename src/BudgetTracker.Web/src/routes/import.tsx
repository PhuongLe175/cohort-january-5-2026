import { FileUpload } from '../features/transactions';
import Header from '../shared/components/layout/Header';

export default function Import() {
  return (
    <div className="px-4 py-6 sm:px-0">
      <Header
        title="Import Transactions"
        subtitle="Upload your bank statement CSV file to import transactions"
      />

      <div className="mt-6">
        <FileUpload />
      </div>
    </div>
  );
}