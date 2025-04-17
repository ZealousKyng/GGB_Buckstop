/*
 * Minesweeper
 * 
 * Adapted from BrofessorTec/SE1Mineswepper
 * https://github.com/BrofessorTec/SE1Mineswepper
 * 
 * Spring 2025, ETSU
 */

// Game variables
const canvas = document.getElementById('game');
const context = canvas.getContext('2d');
const grid = 30; // Size of each cell
let rows = 10;
let cols = 10;
let mines = 10;
let mineField = [];
let revealed = [];
let flagged = [];
let gameOver = false;
let gameWon = false;

// Initialize the game
function init() {
    // Create arrays
    mineField = Array(rows).fill().map(() => Array(cols).fill(0));
    revealed = Array(rows).fill().map(() => Array(cols).fill(false));
    flagged = Array(rows).fill().map(() => Array(cols).fill(false));
    gameOver = false;
    gameWon = false;
    
    // Place mines randomly
    let minesPlaced = 0;
    while (minesPlaced < mines) {
        const row = Math.floor(Math.random() * rows);
        const col = Math.floor(Math.random() * cols);
        
        if (mineField[row][col] !== -1) {
            mineField[row][col] = -1;
            minesPlaced++;
            
            // Update adjacent cell counts
            for (let r = Math.max(0, row - 1); r <= Math.min(rows - 1, row + 1); r++) {
                for (let c = Math.max(0, col - 1); c <= Math.min(cols - 1, col + 1); c++) {
                    if (mineField[r][c] !== -1) {
                        mineField[r][c]++;
                    }
                }
            }
        }
    }
    
    draw();
}

// Draw the game board
function draw() {
    // Clear canvas
    context.clearRect(0, 0, canvas.width, canvas.height);
    
    // Draw cells
    for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
            const x = col * grid;
            const y = row * grid;
            
            // Draw cell background
            if (revealed[row][col]) {
                context.fillStyle = '#ddd';
            } else {
                context.fillStyle = '#aaa';
            }
            context.fillRect(x, y, grid, grid);
            
            // Draw grid lines
            context.strokeStyle = '#888';
            context.strokeRect(x, y, grid, grid);
            
            // Draw cell content
            if (revealed[row][col]) {
                if (mineField[row][col] === -1) {
                    // Draw mine
                    context.fillStyle = 'black';
                    context.beginPath();
                    context.arc(x + grid/2, y + grid/2, grid/4, 0, Math.PI * 2);
                    context.fill();
                } else if (mineField[row][col] > 0) {
                    // Draw number
                    const colors = ['blue', 'green', 'red', 'purple', 'maroon', 'turquoise', 'black', 'gray'];
                    context.fillStyle = colors[mineField[row][col] - 1] || 'black';
                    context.font = '20px Arial';
                    context.textAlign = 'center';
                    context.textBaseline = 'middle';
                    context.fillText(mineField[row][col], x + grid/2, y + grid/2);
                }
            } else if (flagged[row][col]) {
                // Draw flag
                context.fillStyle = 'red';
                context.beginPath();
                context.moveTo(x + grid/4, y + grid/4);
                context.lineTo(x + grid/4, y + 3*grid/4);
                context.lineTo(x + 3*grid/4, y + grid/2);
                context.fill();
            }
        }
    }
    
    // Draw game over message if applicable
    if (gameOver) {
        context.fillStyle = 'rgba(0, 0, 0, 0.5)';
        context.fillRect(0, 0, canvas.width, canvas.height);
        
        context.fillStyle = 'white';
        context.font = '30px Arial';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.fillText('Game Over!', canvas.width / 2, canvas.height / 2 - 20);
        context.font = '20px Arial';
        context.fillText('Click to play again', canvas.width / 2, canvas.height / 2 + 20);
    } else if (gameWon) {
        context.fillStyle = 'rgba(0, 200, 0, 0.5)';
        context.fillRect(0, 0, canvas.width, canvas.height);
        
        context.fillStyle = 'white';
        context.font = '30px Arial';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.fillText('You Win!', canvas.width / 2, canvas.height / 2 - 20);
        context.font = '20px Arial';
        context.fillText('Click to play again', canvas.width / 2, canvas.height / 2 + 20);
    }
}

// Reveal cell and adjacent cells if empty
function revealCell(row, col) {
    if (row < 0 || row >= rows || col < 0 || col >= cols || revealed[row][col] || flagged[row][col]) {
        return;
    }
    
    revealed[row][col] = true;
    
    if (mineField[row][col] === -1) {
        // Game over if mine is revealed
        revealAllMines();
        gameOver = true;
    } else if (mineField[row][col] === 0) {
        // Reveal adjacent cells if empty
        for (let r = Math.max(0, row - 1); r <= Math.min(rows - 1, row + 1); r++) {
            for (let c = Math.max(0, col - 1); c <= Math.min(cols - 1, col + 1); c++) {
                revealCell(r, c);
            }
        }
    }
    
    // Check for win condition
    checkWin();
}

// Reveal all mines on game over
function revealAllMines() {
    for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
            if (mineField[row][col] === -1) {
                revealed[row][col] = true;
            }
        }
    }
}

// Check if player has won
function checkWin() {
    for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
            if (mineField[row][col] !== -1 && !revealed[row][col]) {
                return;
            }
        }
    }
    gameWon = true;
}

// Handle mouse clicks
canvas.addEventListener('click', function(e) {
    if (gameOver || gameWon) {
        init();
        return;
    }
    
    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    const col = Math.floor(x / grid);
    const row = Math.floor(y / grid);
    
    revealCell(row, col);
    draw();
});

// Handle right clicks for flagging
canvas.addEventListener('contextmenu', function(e) {
    e.preventDefault();
    
    if (gameOver || gameWon) {
        return;
    }
    
    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    const col = Math.floor(x / grid);
    const row = Math.floor(y / grid);
    
    if (!revealed[row][col]) {
        flagged[row][col] = !flagged[row][col];
        draw();
    }
});

// Show game instructions
function showInstructions() {
    context.fillStyle = 'rgba(0, 0, 0, 0.7)';
    context.fillRect(0, 0, canvas.width, canvas.height);
    
    context.fillStyle = 'white';
    context.font = '24px Arial';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText('Minesweeper', canvas.width / 2, 50);
    
    context.font = '16px Arial';
    context.fillText('Left Click to reveal a cell', canvas.width / 2, 100);
    context.fillText('Right Click to place/remove a flag', canvas.width / 2, 130);
    context.fillText('Avoid mines and find all safe cells to win!', canvas.width / 2, 160);
    
    context.font = '20px Arial';
    context.fillText('Click to start', canvas.width / 2, canvas.height - 50);
}

// Start the game with instructions
showInstructions();
canvas.addEventListener('click', function startGame() {
    canvas.removeEventListener('click', startGame);
    init();
});

// Adjust canvas size based on grid and cell count
canvas.width = cols * grid;
canvas.height = rows * grid; 