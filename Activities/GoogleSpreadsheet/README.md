Guidelines for using GoogleSpreadsheet Activities

1. Google Sheet Application Scope

    a) Authentication

    - To use this app scope you'll need a google service account, an encryption key and a password to encrypt that key.

    b) Spreadsheet calls

    - To make requests to a GoogleSpreadsheet you'll need to specify the SpreadsheetId

2. Read Range

    - Needs to have specified: Sheet, Range and optional IncludeHeaders
    - The output of this will be a DataTable object (Result property)
    - If you don't specify the Sheet, it'll use the default sheet of the GoogleSpreadsheet

3. Write Range

    - Needs to have specified: Sheet, Range, StartingCell and optional IncludeHeaders


Note: both read range and write should be very similar in usage as the ones from the excel package.