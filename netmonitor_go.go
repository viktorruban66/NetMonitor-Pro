// netmonitor_go.go — монитор сети в реальном времени на Go (консоль + ASCII)

package main

import (
	"bufio"
	"fmt"
	"math"
	"math/rand"
	"os"
	"strconv"
	"strings"
	"time"
)

type NetMonitor struct {
	downHistory []float64
	upHistory   []float64
	timeHistory []int64
	totalDown   float64
	totalUp     float64
	peakDown    float64
	peakUp      float64
	historySize int
}

func NewNetMonitor() *NetMonitor {
	n := &NetMonitor{
		downHistory: make([]float64, 0, 60),
		upHistory:   make([]float64, 0, 60),
		timeHistory: make([]int64, 0, 60),
		historySize: 60,
	}
	for i := 0; i < 10; i++ {
		n.downHistory = append(n.downHistory, 0)
		n.upHistory = append(n.upHistory, 0)
		n.timeHistory = append(n.timeHistory, time.Now().Unix())
	}
	return n
}

func (n *NetMonitor) update() {
	// Симуляция
	down := rand.Float64()*13 + 2
	up := rand.Float64()*7 + 1

	n.downHistory = append(n.downHistory, down)
	n.upHistory = append(n.upHistory, up)
	n.timeHistory = append(n.timeHistory, time.Now().Unix())
	if len(n.downHistory) > n.historySize {
		n.downHistory = n.downHistory[1:]
		n.upHistory = n.upHistory[1:]
		n.timeHistory = n.timeHistory[1:]
	}
	n.totalDown += down
	n.totalUp += up
	if down > n.peakDown {
		n.peakDown = down
	}
	if up > n.peakUp {
		n.peakUp = up
	}
}

func (n *NetMonitor) display() {
	// Очистка экрана
	fmt.Print("\033[H\033[2J")
	fmt.Println("📊 NetMonitor Pro — Go Edition")
	if len(n.downHistory) > 0 {
		fmt.Printf("⬇ Загрузка: %.1f Мбит/с\n", n.downHistory[len(n.downHistory)-1])
		fmt.Printf("⬆ Выгрузка: %.1f Мбит/с\n", n.upHistory[len(n.upHistory)-1])
	}
	fmt.Printf("Трафик: %.1f/%.1f МБ\n", n.totalDown/8, n.totalUp/8)
	fmt.Printf("Пик: %.1f Мбит/с   Средняя: %.1f Мбит/с\n", n.peakDown, n.average(n.downHistory))

	// ASCII график
	fmt.Println("\nГрафик скорости за последние 60 сек (синий — загрузка, красный — выгрузка):")
	if len(n.downHistory) > 1 {
		const width = 50
		const height = 10
		maxVal := 0.0
		for _, v := range n.downHistory {
			if v > maxVal {
				maxVal = v
			}
		}
		for _, v := range n.upHistory {
			if v > maxVal {
				maxVal = v
			}
		}
		if maxVal < 10 {
			maxVal = 10
		}
		dataDown := n.downHistory
		dataUp := n.upHistory
		step := float64(len(dataDown)-1) / float64(width-1)

		gridDown := make([][]rune, height)
		gridUp := make([][]rune, height)
		for i := range gridDown {
			gridDown[i] = make([]rune, width)
			gridUp[i] = make([]rune, width)
			for j := range gridDown[i] {
				gridDown[i][j] = ' '
				gridUp[i][j] = ' '
			}
		}
		for i := 0; i < width; i++ {
			idx := int(float64(i) * step)
			if idx >= len(dataDown) {
				idx = len(dataDown) - 1
			}
			valDown := dataDown[idx]
			valUp := dataUp[idx]
			yDown := int((valDown / maxVal) * float64(height-1))
			yUp := int((valUp / maxVal) * float64(height-1))
			if yDown >= height {
				yDown = height - 1
			}
			if yUp >= height {
				yUp = height - 1
			}
			gridDown[height-1-yDown][i] = '█'
			gridUp[height-1-yUp][i] = '█'
		}
		// Печать
		fmt.Print("      ")
		for j := 0; j < width; j++ {
			if j%5 == 0 {
				fmt.Print(j/5)
			} else {
				fmt.Print(" ")
			}
		}
		fmt.Println()
		for i := 0; i < height; i++ {
			fmt.Printf("%3d%% ", int(float64(height-1-i)/float64(height-1)*100))
			for j := 0; j < width; j++ {
				if gridDown[i][j] == '█' && gridUp[i][j] == '█' {
					fmt.Print("\033[35m█\033[0m") // фиолетовый (пересечение)
				} else if gridDown[i][j] == '█' {
					fmt.Print("\033[34m█\033[0m")
				} else if gridUp[i][j] == '█' {
					fmt.Print("\033[31m█\033[0m")
				} else {
					fmt.Print(" ")
				}
			}
			fmt.Println()
		}
	}
	fmt.Println("\nКоманды: export, reset, exit")
}

func (n *NetMonitor) average(data []float64) float64 {
	if len(data) == 0 {
		return 0
	}
	sum := 0.0
	for _, v := range data {
		sum += v
	}
	return sum / float64(len(data))
}

func (n *NetMonitor) exportCSV() {
	if len(n.downHistory) < 2 {
		fmt.Println("Недостаточно данных для экспорта")
		return
	}
	filename := fmt.Sprintf("net_data_%d.csv", time.Now().Unix())
	file, err := os.Create(filename)
	if err != nil {
		fmt.Println("Ошибка создания файла:", err)
		return
	}
	defer file.Close()
	file.WriteString("Время,Загрузка(Мбит/с),Выгрузка(Мбит/с)\n")
	for i := 0; i < len(n.downHistory); i++ {
		file.WriteString(fmt.Sprintf("%d,%.2f,%.2f\n", n.timeHistory[i], n.downHistory[i], n.upHistory[i]))
	}
	fmt.Printf("Данные сохранены в %s\n", filename)
}

func (n *NetMonitor) reset() {
	n.downHistory = n.downHistory[:0]
	n.upHistory = n.upHistory[:0]
	n.timeHistory = n.timeHistory[:0]
	n.totalDown = 0
	n.totalUp = 0
	n.peakDown = 0
	n.peakUp = 0
	for i := 0; i < 10; i++ {
		n.downHistory = append(n.downHistory, 0)
		n.upHistory = append(n.upHistory, 0)
		n.timeHistory = append(n.timeHistory, time.Now().Unix())
	}
	fmt.Println("Статистика сброшена")
}

func main() {
	rand.Seed(time.Now().UnixNano())
	monitor := NewNetMonitor()
	scanner := bufio.NewScanner(os.Stdin)
	fmt.Println("📊 NetMonitor Pro — Go Edition")
	fmt.Println("Нажмите Enter для обновления, или введите команду: export, reset, exit")
	for {
		monitor.update()
		monitor.display()
		fmt.Print("> ")
		if !scanner.Scan() {
			break
		}
		line := strings.TrimSpace(scanner.Text())
		switch line {
		case "export":
			monitor.exportCSV()
		case "reset":
			monitor.reset()
		case "exit":
			fmt.Println("До свидания!")
			return
		default:
			// обновление
		}
	}
}
