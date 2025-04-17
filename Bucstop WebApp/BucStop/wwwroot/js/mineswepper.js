/*
 * Minesweeper
 * 
 * Adapted from mineswepper-reference
 * 
 * Spring 2025, ETSU
 */

document.write(`
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Mineswepper</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            display: flex;
            flex-direction: column;
            align-items: center;
            background-color: #f8f9fa;
            height: 100vh;
            overflow: auto;
        }
        h1 {
            text-align: center;
            margin: 20px 0px 30px auto;
            color: #333;
            text-shadow: 1px 1px 2px rgba(0,0,0,0.1);
        }
        .frame {
            width: 400px;
            height: 800px;
            margin: auto;
            border-width: 1px;
            border-color: black;
            border-style: solid;
            box-shadow: 5px 5px 5px lightgrey;
        }
        .game-board {
            width: 400px;
            height: 400px;
            margin: auto;
            border-width: 2px 0px 2px 0px;
            border-color: black;
            border-style: solid;
            display: grid;
            gap: 1px;
        }
        .top, .bottom {
            width: 400px;
            height: 200px;
            margin: auto;
            background-color: #C0C0C0;
        }
        .cell {
            width: 39px;
            height: 39px;
            border: 1px solid #000000;
            text-align: center;
            line-height: 40px;
            font-size: 20px;
            cursor: pointer;
            background-color: #808080;
        }
        .cell:hover {
            background-color: #bbb;
        }
        .cell.revealed {
            background-color: #C0C0C0;
        }
        .cell.bomb {
            background-color: #d80c0c;
        }
        .cell.flagged {
            background-color: #f1c40f;
            color: black;
        }
        #restart-button {
            position: relative;
            top: 10px; 
            left: 50%; 
            transform: translateX(-50%); 
            font-size: 16px;
            color: white;
            background-color: #007BFF; 
            border: none;
            border-radius: 5px;
            cursor: pointer;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            transition: background-color 0.3s ease, box-shadow 0.3s ease;
            padding: 8px 16px;
        }
        #restart-button:hover {
            background-color: #0056b3; 
            box-shadow: 0 6px 8px rgba(0, 0, 0, 0.15); 
        }
        #timer {
            display: block;
            text-align: center;
            font-size: 28px;
            color: #333;
            margin-top: 20px;
        }
        #mine-count {
            display: inline-block;
            text-align: center;
            font-size: 22px;
            color: #333;
            margin: 0;
            margin-top: 10px;
            margin-left: 67px;
        }
        #reveal-count {
            display: inline-block;
            font-size: 22px; 
            color: #333;
            text-align: center;
            margin: 0;
            margin-top: 10px;
            margin-left: 10px;
        }
        #flag-count {
            display: inline-block;
            text-align: center;
            font-size: 22px;
            color: #333;
            margin: 0 10px;
            margin-top: 10px;
        }
        * {
            user-select: none;
            -webkit-user-select: none;
            -ms-user-select: none;
        }
        a, input, textarea {
            -webkit-touch-callout: none;
        }
        .instructions {
            margin: 20px auto;
            max-width: 400px;
            width: 90%;
            text-align: left;
            line-height: 1.5;
            padding: 15px;
            background: #f5f5f5;
            border-radius: 5px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .instructions h2 {
            margin-top: 0;
            color: #333;
        }
        .instructions ul {
            padding-left: 20px;
        }
        .name-button {
            margin-top: 10px;
            padding: 5px 10px;
            background-color: #28a745;
            color: white;
            border: none;
            border-radius: 3px;
            cursor: pointer;
        }
        #nameInput {
            padding: 5px;
            margin-top: 10px;
            text-align: center;
            width: 150px;
        }
    </style>
</head>
<body>
    <div id="frame" class="frame">
        <div class="top">
            <div class="controls">
                <button id="restart-button">Restart Game</button>
                <span id="timer">Time: 0:00</span>
                <span id="mine-count">💣: 0</span>
                <span id="reveal-count">Revealed: 00</span>
                <span id="flag-count">🚩: 0</span>
            </div>
        </div>
        
        <div class="game-board" id="game-board">
            <!-- Game board will be generated here -->
        </div>
        
        <div class="bottom">
            <div style="text-align: center; margin-top: 30px;">
                <input type="text" id="nameInput" maxlength="3" placeholder="Enter 3-character name" style="display: none;">
                <br>
                <button onclick="saveName()" class="name-button" style="display: none;">Save Name</button>
            </div>
        </div>
    </div>
    
    <div class="instructions">
        <h2>How to Play:</h2>
        <ul>
            <li>Left-click to reveal a cell</li>
            <li>Right-click to place or remove a flag on suspected mines</li>
            <li>Numbers indicate how many mines are adjacent to that cell</li>
            <li>Find all safe cells without clicking on any mines to win!</li>
        </ul>
    </div>

    <script>
        // Tile class definition
        class Tile {
            constructor() {
                this.domElement = document.createElement('div');
                this.domElement.classList.add('cell');
                this.mine = false;
                this.revealed = false;
                this.flagged = false;
                this.adjacentMines = 0;
                this.wasLongPress = false;
            }
            
            setMine() {
                this.mine = true;
            }
            
            setAdjacentMines(count) {
                this.adjacentMines = count;
            }
            
            reveal(tileArray, row, col) {
                if (this.revealed || gameOver || this.flagged) return;
                
                this.revealed = true;
                this.domElement.classList.add('revealed');
                
                // First-click logic
                if (firstClick) {
                    startTimer();
                    bombSpots = generateMinesAfterFirstClick(row, col, tileArray.length, tileArray[0].length);
                    populateMines(tileArray, bombSpots);
                    checkNeighborMines(tileArray);
                    firstClick = false;
                }
                
                // Reveal logic
                if (this.mine) {
                    this.domElement.classList.add('bomb');
                    this.domElement.textContent = '💣';
                    hitMine();
                } else if (this.adjacentMines === 0) {
                    this.domElement.textContent = '';
                    this.revealAdjacentEmpty(tileArray, row, col);
                    this.gameWinCheck();
                } else {
                    this.domElement.textContent = this.adjacentMines.toString();
                    this.gameWinCheck();
                }
            }
            
            gameWinCheck() {
                if (revealedTilesForWin == 89) {
                    gameOver = true;
                    timerStarted = false;
                    clearInterval(timeInterval);
                    displayEndGameUI();
                    
                    revealedTilesForWin++;
                    document.getElementById('reveal-count').textContent = \`Revealed: \${String(revealedTilesForWin).padStart(2, '0')}\`;
                    console.log("Game Over, you win! " + document.getElementById('timer').textContent);
                } else {
                    revealedTilesForWin++;
                    document.getElementById('reveal-count').textContent = \`Revealed: \${String(revealedTilesForWin).padStart(2, '0')}\`;
                }
            }
            
            toggleFlag() {
                if (this.revealed) return;
                this.flagged = !this.flagged;
                
                if (this.flagged) {
                    this.domElement.classList.add('flagged');
                    this.domElement.textContent = '🚩';
                    totalFlags++;
                } else {
                    this.domElement.classList.remove('flagged');
                    this.domElement.textContent = '';
                    totalFlags--;
                }
                updateFlagCount();
            }
            
            revealAdjacentEmpty(tileArray, row, col) {
                const directions = [
                    [-1, -1], [-1, 0], [-1, 1],
                    [0, -1],          [0, 1],
                    [1, -1], [1, 0], [1, 1],
                ];
                
                directions.forEach(([dx, dy]) => {
                    const newRow = row + dx;
                    const newCol = col + dy;
                    
                    if (
                        newRow >= 0 && newRow < tileArray.length &&
                        newCol >= 0 && newCol < tileArray[0].length
                    ) {
                        const neighbor = tileArray[newRow][newCol];
                        if (!neighbor.revealed && !neighbor.mine) {
                            neighbor.reveal(tileArray, newRow, newCol);
                        }
                    }
                });
            }
        }
        
        // Game variables
        const gameBoard = document.getElementById('game-board');
        const gridRows = 10;
        const gridCols = 10;
        let difficulty = 1;
        let timeElapsed = 0;
        let timeInterval = null;
        let timerStarted = false;
        let totalFlags = 0;
        let firstClick = true;
        let bombSpots = [];
        let gameOver = false;
        let revealedTilesForWin = 0;
        
        // Function to update the timer
        function updateTimer() {
            timeElapsed++;
            const minutes = Math.floor(timeElapsed / 60);
            const seconds = timeElapsed % 60;
            document.getElementById('timer').textContent = \`Time: \${minutes}:\${seconds.toString().padStart(2, '0')}\`;
            
            if (timeElapsed >= 599) {
                clearInterval(timeInterval);
            }
        }
        
        // Function to start the timer
        function startTimer() {
            timeElapsed = 0;
            if (!timerStarted) {
                timerStarted = true;
                timeInterval = setInterval(updateTimer, 1000);
            }
        }
        
        // Function to update the flag count display
        function updateFlagCount() {
            document.getElementById('flag-count').textContent = \`🚩: \${totalFlags}\`;
        }
        
        // Function to initialize the game grid
        function initializeGame() {
            gameBoard.innerHTML = '';
            firstClick = true;
            gameOver = false;
            revealedTilesForWin = 0;
            totalFlags = 0;
            updateFlagCount();
            if (timeInterval) clearInterval(timeInterval);
            document.getElementById('timer').textContent = 'Time: 0:00';
            timerStarted = false;
            document.getElementById('reveal-count').textContent = 'Revealed: 00';
            
            gameBoard.style.gridTemplateColumns = \`repeat(\${gridCols}, 39px)\`;
            gameBoard.style.gridTemplateRows = \`repeat(\${gridRows}, 39px)\`;
            
            const tileArray = createEmptyGrid(gridRows, gridCols);
            
            tileArray.forEach((row, rowIndex) => {
                row.forEach((tile, colIndex) => {
                    gameBoard.appendChild(tile.domElement);
                    
                    tile.domElement.addEventListener('click', () => {
                        if (gameOver) return;
                        
                        if (firstClick) {
                            startTimer();
                            bombSpots = generateMinesAfterFirstClick(rowIndex, colIndex, gridRows, gridCols);
                            populateMines(tileArray, bombSpots);
                            checkNeighborMines(tileArray);
                            firstClick = false;
                        }
                        
                        tile.reveal(tileArray, rowIndex, colIndex);
                        checkGameOver(tileArray);
                    });
                    
                    tile.domElement.addEventListener('contextmenu', function (e) {
                        e.preventDefault();
                        if (!gameOver) {
                            tile.toggleFlag();
                        }
                    });
                    
                    // Add touch events for mobile
                    let touchStartTimer;
                    tile.domElement.addEventListener('touchstart', function (e) {
                        e.preventDefault();
                        tile.wasLongPress = false;
                        touchStartTimer = setTimeout(() => {
                            if (!tile.revealed) {
                                tile.toggleFlag();
                                tile.wasLongPress = true;
                            }
                        }, 500);
                    });
                    
                    tile.domElement.addEventListener('touchend', function (e) {
                        e.preventDefault();
                        clearTimeout(touchStartTimer);
                        if (!tile.wasLongPress && !tile.flagged && !gameOver) {
                            tile.reveal(tileArray, rowIndex, colIndex);
                        }
                    });
                    
                    tile.domElement.addEventListener('touchcancel', function () {
                        clearTimeout(touchStartTimer);
                    });
                });
            });
            updateMineCountInHTML();
        }
        
        // Create an empty grid with no mines
        function createEmptyGrid(rows, cols) {
            return create2DArray(rows, cols, []);
        }
        
        // Function to create 2D array of tiles
        function create2DArray(rows, cols, bombSpots) {
            const arr = new Array(rows);
            
            for (let i = 0; i < rows; i++) {
                arr[i] = new Array(cols);
                
                for (let j = 0; j < cols; j++) {
                    const tile = new Tile();
                    arr[i][j] = tile;
                    
                    const index = i * cols + j;
                    if (bombSpots.includes(index)) {
                        tile.setMine();
                    }
                    
                    tile.domElement.__tileObj = tile;
                }
            }
            
            return arr;
        }
        
        // Generate mines, avoiding the first clicked tile and its neighbors
        function generateMinesAfterFirstClick(row, col, rows, cols) {
            const excludedTiles = getExcludedTiles(row, col, rows, cols);
            const bombSpots = [];
            const totalCells = rows * cols;
            const bombCount = 10 * difficulty;
            
            while (bombSpots.length < bombCount) {
                const bombLocation = Math.floor(Math.random() * totalCells);
                if (!excludedTiles.includes(bombLocation) && !bombSpots.includes(bombLocation)) {
                    bombSpots.push(bombLocation);
                }
            }
            
            return bombSpots;
        }
        
        // Get tiles to exclude from bomb placement (clicked tile and neighbors)
        function getExcludedTiles(row, col, rows, cols) {
            const excluded = [];
            for (let i = -1; i <= 1; i++) {
                for (let j = -1; j <= 1; j++) {
                    const newRow = row + i;
                    const newCol = col + j;
                    if (newRow >= 0 && newRow < rows && newCol >= 0 && newCol < cols) {
                        excluded.push(newRow * cols + newCol);
                    }
                }
            }
            return excluded;
        }
        
        // Populate the grid with mines based on bombSpots
        function populateMines(tileArray, bombSpots) {
            bombSpots.forEach((bombIndex) => {
                const row = Math.floor(bombIndex / gridCols);
                const col = bombIndex % gridCols;
                tileArray[row][col].setMine();
            });
        }
        
        // Calculate adjacent mines for each tile
        function checkNeighborMines(tileArray) {
            for (let row = 0; row < gridRows; row++) {
                for (let col = 0; col < gridCols; col++) {
                    if (!tileArray[row][col].mine) {
                        let mineCount = 0;
                        const directions = [
                            [-1, -1], [-1, 0], [-1, 1],
                            [0, -1],          [0, 1],
                            [1, -1], [1, 0], [1, 1],
                        ];
                        
                        directions.forEach(([dx, dy]) => {
                            const newRow = row + dx;
                            const newCol = col + dy;
                            if (
                                newRow >= 0 && newRow < gridRows &&
                                newCol >= 0 && newCol < gridCols &&
                                tileArray[newRow][newCol].mine
                            ) {
                                mineCount++;
                            }
                        });
                        
                        tileArray[row][col].setAdjacentMines(mineCount);
                    }
                }
            }
        }
        
        // End the game when revealing a mine tile
        function hitMine() {
            gameOver = true;
            timerStarted = false;
            clearInterval(timeInterval);
            
            // Reveal all bombs
            const tiles = document.querySelectorAll('.cell');
            tiles.forEach((tile) => {
                const tileObj = tile.__tileObj;
                if (tileObj && tileObj.mine) {
                    tile.textContent = '💣';
                    tile.classList.add('bomb', 'revealed');
                }
                tile.style.pointerEvents = 'none';
            });
            
            // Show the name input field after game ends
            displayEndGameUI();
        }
        
        // Function to update the mine counter based on difficulty
        function updateMineCountInHTML() {
            const mineCount = 10 * difficulty;
            const mineCountElement = document.getElementById('mine-count');
            mineCountElement.textContent = \`💣: \${mineCount}\`;
        }
        
        // Display end game UI
        function displayEndGameUI() {
            document.getElementById("nameInput").style.display = 'block';
            document.querySelector(".name-button").style.display = 'block';
        }
        
        // Restart game functionality
        function restartGame() {
            initializeGame();
            document.getElementById("nameInput").style.display = 'none';
            document.querySelector(".name-button").style.display = 'none';
            updateMineCountInHTML();
        }
        
        // Check game over condition
        function checkGameOver(tileArray) {
            const totalTiles = gridRows * gridCols;
            const bombCount = bombSpots.length;
            let revealedNonBombCount = 0;
            
            tileArray.forEach(row => {
                row.forEach(tile => {
                    if (tile.revealed && !tile.mine) {
                        revealedNonBombCount++;
                    }
                });
            });
            
            if (revealedNonBombCount === (totalTiles - bombCount) || gameOver) {
                gameOver = true;
                displayEndGameUI();
            }
        }
        
        // Save player name
        function saveName() {
            const userName = document.getElementById("nameInput").value.trim();
            
            if (userName.length === 3) {
                localStorage.setItem('minesweeperUsername', userName);
                alert("Name saved: " + userName);
                document.getElementById("nameInput").style.display = 'none';
                document.querySelector(".name-button").style.display = 'none';
            } else {
                alert("Please enter exactly 3 characters for your name.");
            }
        }
        
        // Add restart button functionality
        document.getElementById("restart-button").addEventListener("click", restartGame);
        
        // Initialize the game grid on page load
        initializeGame();
    </script>
</body>
</html>
`); 