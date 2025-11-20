import sqlite3
import os

def clear_database(db_file='leads.db'):
    """
    Clear all records from the leads table in the database.
    
    Args:
        db_file: Path to the SQLite database file
    """
    try:
        # Check if database exists
        if not os.path.exists(db_file):
            print(f"Database file '{db_file}' not found.")
            return
        
        # Connect to the database
        conn = sqlite3.connect(db_file)
        cursor = conn.cursor()
        
        # Get count before deletion
        cursor.execute("SELECT COUNT(*) FROM leads")
        count_before = cursor.fetchone()[0]
        
        if count_before == 0:
            print("Database is already empty.")
            conn.close()
            return
        
        # Delete all records
        cursor.execute("DELETE FROM leads")
        
        # Reset the auto-increment counter (optional, but keeps IDs starting from 1)
        # sqlite_sequence table may not exist if no auto-increment tables have been used
        try:
            cursor.execute("DELETE FROM sqlite_sequence WHERE name='leads'")
        except sqlite3.Error:
            # sqlite_sequence doesn't exist, which is fine
            pass
        
        # Commit the changes
        conn.commit()
        conn.close()
        
        print(f"Successfully cleared {count_before} leads from the database.")
        print(f"Database file '{db_file}' now has an empty leads table.")
        
    except sqlite3.Error as e:
        print(f"Database error: {e}")
    except Exception as e:
        print(f"Error clearing database: {e}")

if __name__ == "__main__":
    clear_database()

