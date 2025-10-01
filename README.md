# Bank Transaction Importer

A .NET console application that imports Swedish bank transaction CSV files and maps them to Google Sheets budget categories.

## Features

- ✅ Parse Swedish bank transaction CSV files
- ✅ Map transactions to budget categories using configurable rules
- ✅ Support for Swedish date and number formats
- ✅ Detailed logging and transaction mapping results
- ✅ Dry-run mode for preview before updating
- 🚧 Google Sheets integration (mock implementation)

## Setup

### 1. Build the Application

```bash
cd src/BankTransactionImporter
dotnet build
```

### 2. Configuration

Configure the application by editing the files in the `config/` folder:

- **`appsettings.json`**: Main application settings
- **`mapping-rules.json`**: Transaction-to-category mapping rules

### 3. Google Sheets Setup (TODO)

The Google Sheets integration is currently using a mock implementation. To enable real integration:

1. Create a Google Cloud project
2. Enable the Google Sheets API
3. Create credentials (Service Account or OAuth2)
4. Update the `GoogleSheetsService.cs` to use real authentication

## Usage

### Basic Usage

```bash
# Process a transaction file (dry-run mode)
dotnet run -- --file "../../data/Transaktioner_2025-10-01_17-03-34.csv" --dry-run

# Process and update Google Sheets (when implemented)
dotnet run -- --file "../../data/Transaktioner_2025-10-01_17-03-34.csv"

# Specify a different sheet name
dotnet run -- --file "../../data/transactions.csv" --sheet "2025"
```

### Command Line Options

- `--file, -f <path>`: Path to the CSV transaction file (required)
- `--sheet, -s <name>`: Name of the Google Sheets tab (optional)
- `--dry-run, -d`: Preview changes without updating (optional)
- `--help, -h`: Show help message

## CSV File Format

The application expects Swedish bank transaction files with the following columns:

- Radnummer (Row number)
- Clearingnummer (Clearing number)
- Kontonummer (Account number)
- Produkt (Product)
- Valuta (Currency)
- Bokföringsdag (Booking date)
- Transaktionsdag (Transaction date)
- Valutadag (Currency date)
- Referens (Reference)
- Beskrivning (Description)
- Belopp (Amount)
- Bokfört saldo (Booked balance)

## Mapping Rules

Transaction mapping is based on the `Reference` or `Description` fields. Configure mappings in `config/mapping-rules.json`:

```json
{
	"mappingRules": {
		"TELE2": "Tele2",
		"MAT": "Mat",
		"HYRA": "Hyra"
	}
}
```

## Project Structure

```
BankTransactionImporter/
├── src/BankTransactionImporter/          # Main application
│   ├── Models/                           # Data models
│   ├── Services/                         # Business logic services
│   ├── Configuration/                    # Configuration classes
│   ├── Application.cs                    # Main application logic
│   └── Program.cs                        # Entry point
├── config/                               # Configuration files
│   ├── appsettings.json                 # App settings
│   └── mapping-rules.json               # Transaction mapping rules
├── data/                                # Sample data files
└── BankTransactionImporter.sln          # Solution file
```

## Example Output

```
info: BankTransactionImporter.Application[0]
      Starting Bank Transaction Importer
info: BankTransactionImporter.Application[0]
      Loaded 22 transactions from ../../data/Transaktioner_2025-10-01_17-03-34.csv
info: BankTransactionImporter.Application[0]
      === TRANSACTION MAPPING RESULTS ===
info: BankTransactionImporter.Application[0]
        Tele2 (09): 579.00 kr (1 transactions)
info: BankTransactionImporter.Application[0]
        A-kassa (09): 160.00 kr (1 transactions)
info: BankTransactionImporter.Application[0]
        Mat (09): 4,000.00 kr (1 transactions)
```

## Next Steps

1. **Complete Google Sheets Integration**: Implement real authentication and API calls
2. **Enhanced Mapping**: Add fuzzy matching and machine learning for better categorization
3. **Multi-file Processing**: Support batch processing of multiple CSV files
4. **Data Validation**: Add more robust validation and error handling
5. **GUI Interface**: Consider creating a desktop or web interface
6. **Backup & Restore**: Implement backup functionality before making changes

## Dependencies

- .NET 8.0
- CsvHelper (CSV parsing)
- Google.Apis.Sheets.v4 (Google Sheets API)
- Microsoft.Extensions.\* (Configuration, DI, Logging)
