// netmonitor_js.js — монитор сети в реальном времени на JavaScript (Node.js + blessed-contrib)

const blessed = require('blessed');
const contrib = require('blessed-contrib');
const fs = require('fs');

const HISTORY_SIZE = 60;
let downHistory = [];
let upHistory = [];
let timeHistory = [];
let totalDown = 0, totalUp = 0;
let peakDown = 0, peakUp = 0;
let running = true;

function currentTime() {
    return Math.floor(Date.now() / 1000);
}

function generateData() {
    const down = Math.random() * 13 + 2;
    const up = Math.random() * 7 + 1;
    return { down, up };
}

function updateData() {
    const { down, up } = generateData();
    downHistory.push(down);
    upHistory.push(up);
    timeHistory.push(currentTime());
    if (downHistory.length > HISTORY_SIZE) {
        downHistory.shift();
        upHistory.shift();
        timeHistory.shift();
    }
    totalDown += down;
    totalUp += up;
    if (down > peakDown) peakDown = down;
    if (up > peakUp) peakUp = up;
}

// Создание интерфейса
const screen = blessed.screen({
    smartCSR: true,
    title: 'NetMonitor Pro'
});

// Grid для размещения виджетов
const grid = new contrib.grid({ rows: 4, cols: 2, screen: screen });

// Метки для информации
const infoBox = grid.set(0, 0, 1, 2, blessed.box, {
    content: '📊 NetMonitor Pro — JavaScript Edition\n⬇ Загрузка: 0.0 Мбит/с  ⬆ Выгрузка: 0.0 Мбит/с\nТрафик: 0.0/0.0 МБ  Пик: 0.0 Мбит/с  Средняя: 0.0 Мбит/с',
    style: {
        fg: 'white',
        bg: 'black'
    }
});

// График линии
const lineChart = grid.set(1, 0, 3, 2, contrib.line, {
    style: {
        line: "yellow",
        text: "green",
        baseline: "black"
    },
    xLabelPadding: 3,
    xPadding: 5,
    showLegend: true,
    legend: { width: 10 },
    wholeNumbersOnly: false,
    label: 'Скорость сети (Мбит/с)'
});

let lineData = {
    x: [],
    y: [],
    title: 'Загрузка'
};
let lineData2 = {
    x: [],
    y: [],
    title: 'Выгрузка'
};

// Обновление данных
function refresh() {
    updateData();
    const now = new Date();
    const timeStr = now.toLocaleTimeString();
    const lastDown = downHistory[downHistory.length-1] || 0;
    const lastUp = upHistory[upHistory.length-1] || 0;
    const avgDown = downHistory.length ? downHistory.reduce((a,b)=>a+b,0)/downHistory.length : 0;
    infoBox.setContent(
        `📊 NetMonitor Pro — JavaScript Edition\n` +
        `⬇ Загрузка: ${lastDown.toFixed(1)} Мбит/с  ⬆ Выгрузка: ${lastUp.toFixed(1)} Мбит/с\n` +
        `Трафик: ${(totalDown/8).toFixed(1)}/${(totalUp/8).toFixed(1)} МБ  Пик: ${peakDown.toFixed(1)} Мбит/с  Средняя: ${avgDown.toFixed(1)} Мбит/с`
    );

    // Обновление графика
    const xData = timeHistory.map(t => new Date(t*1000).toLocaleTimeString());
    lineData.x = xData;
    lineData.y = downHistory;
    lineData2.x = xData;
    lineData2.y = upHistory;
    lineChart.setData([lineData, lineData2]);

    screen.render();
}

// Запуск обновления каждую секунду
setInterval(refresh, 1000);

// Обработка клавиш
screen.key(['escape', 'q', 'C-c'], function(ch, key) {
    running = false;
    return process.exit(0);
});

screen.key(['e'], function() {
    // Экспорт в CSV
    if (downHistory.length < 2) {
        infoBox.setContent('Недостаточно данных для экспорта');
        screen.render();
        return;
    }
    const filename = `net_data_${Date.now()}.csv`;
    let csv = 'Время,Загрузка(Мбит/с),Выгрузка(Мбит/с)\n';
    for (let i = 0; i < downHistory.length; i++) {
        csv += `${timeHistory[i]},${downHistory[i].toFixed(2)},${upHistory[i].toFixed(2)}\n`;
    }
    fs.writeFileSync(filename, csv);
    infoBox.setContent(`Данные сохранены в ${filename}`);
    screen.render();
});

screen.key(['r'], function() {
    // Сброс
    downHistory = [];
    upHistory = [];
    timeHistory = [];
    totalDown = 0;
    totalUp = 0;
    peakDown = 0;
    peakUp = 0;
    infoBox.setContent('Статистика сброшена');
    screen.render();
});

screen.render();

// Инструкция
console.log('NetMonitor Pro запущен. Используйте клавиши: e - экспорт CSV, r - сброс, q - выход.');
