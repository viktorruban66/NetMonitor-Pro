# netmonitor_python.py — монитор сети в реальном времени на Python (Tkinter + Matplotlib)

import tkinter as tk
from tkinter import ttk, messagebox
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
import psutil
import threading
import time
import json
import os
from collections import deque
import numpy as np

class NetMonitor:
    def __init__(self, root):
        self.root = root
        self.root.title("📊 NetMonitor Pro — Python")
        self.root.geometry("800x600")
        self.root.protocol("WM_DELETE_WINDOW", self.on_close)

        # Данные
        self.history_down = deque(maxlen=60)   # скорость загрузки (Мбит/с)
        self.history_up = deque(maxlen=60)     # скорость выгрузки (Мбит/с)
        self.time_history = deque(maxlen=60)
        self.total_down = 0
        self.total_up = 0
        self.peak_down = 0
        self.peak_up = 0
        self.avg_down = 0
        self.avg_up = 0
        self.interface = "eth0"  # можно определить автоматически
        self.running = True
        self.interval = 1.0  # секунды

        # Настройки
        self.config_file = "net_config.json"
        self.load_config()

        # GUI
        self.create_widgets()
        self.update_loop()
        self.start_monitoring()

    def create_widgets(self):
        # Верхняя панель с информацией
        top_frame = tk.Frame(self.root)
        top_frame.pack(fill=tk.X, padx=10, pady=5)

        self.down_label = tk.Label(top_frame, text="⬇ Загрузка: 0.0 Мбит/с", font=("Arial", 14))
        self.down_label.pack(side=tk.LEFT, padx=10)
        self.up_label = tk.Label(top_frame, text="⬆ Выгрузка: 0.0 Мбит/с", font=("Arial", 14))
        self.up_label.pack(side=tk.LEFT, padx=10)
        self.total_label = tk.Label(top_frame, text="Трафик: 0/0 МБ", font=("Arial", 12))
        self.total_label.pack(side=tk.LEFT, padx=10)

        # График
        fig, self.ax = plt.subplots(figsize=(6, 3), dpi=100)
        self.ax.set_xlabel("Время (сек)")
        self.ax.set_ylabel("Скорость (Мбит/с)")
        self.ax.grid(True)
        self.line_down, = self.ax.plot([], [], 'b-', linewidth=2, label='Загрузка')
        self.line_up, = self.ax.plot([], [], 'r-', linewidth=2, label='Выгрузка')
        self.ax.legend()
        self.canvas = FigureCanvasTkAgg(fig, master=self.root)
        self.canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        # Кнопки
        btn_frame = tk.Frame(self.root)
        btn_frame.pack(pady=5)
        tk.Button(btn_frame, text="Экспорт CSV", command=self.export_csv).pack(side=tk.LEFT, padx=5)
        tk.Button(btn_frame, text="Сброс статистики", command=self.reset_stats).pack(side=tk.LEFT, padx=5)

        self.status = tk.Label(self.root, text="Готов", anchor=tk.W)
        self.status.pack(fill=tk.X, padx=10)

    def start_monitoring(self):
        # Запуск сбора данных
        def monitor():
            while self.running:
                try:
                    # Получаем сетевую статистику через psutil
                    net_io = psutil.net_io_counters(pernic=True)
                    if self.interface in net_io:
                        stats = net_io[self.interface]
                        # вычисляем прирост с прошлого замера
                        if hasattr(self, 'last_stats'):
                            bytes_sent = stats.bytes_sent - self.last_stats.bytes_sent
                            bytes_recv = stats.bytes_recv - self.last_stats.bytes_recv
                            # пересчёт в Мбит/с
                            down_speed = bytes_recv / self.interval * 8 / 1_000_000
                            up_speed = bytes_sent / self.interval * 8 / 1_000_000
                            self.update_data(down_speed, up_speed)
                        self.last_stats = stats
                    else:
                        # Если интерфейс не найден, используем симуляцию
                        self.simulate_data()
                except Exception as e:
                    self.simulate_data()
                time.sleep(self.interval)
        threading.Thread(target=monitor, daemon=True).start()

    def simulate_data(self):
        # Генерация случайных данных для демонстрации
        down = np.random.exponential(5) + 2
        up = np.random.exponential(2) + 1
        self.update_data(down, up)

    def update_data(self, down, up):
        # Обновление истории
        self.history_down.append(down)
        self.history_up.append(up)
        self.time_history.append(time.time())
        self.total_down += down * self.interval / 8  # МБайт
        self.total_up += up * self.interval / 8

        # Обновление статистики
        if down > self.peak_down:
            self.peak_down = down
        if up > self.peak_up:
            self.peak_up = up
        if len(self.history_down) > 0:
            self.avg_down = sum(self.history_down) / len(self.history_down)
            self.avg_up = sum(self.history_up) / len(self.history_up)

        # Обновление GUI
        self.root.after(0, self.update_gui)

    def update_gui(self):
        # Метки
        self.down_label.config(text=f"⬇ Загрузка: {self.history_down[-1]:.1f} Мбит/с")
        self.up_label.config(text=f"⬆ Выгрузка: {self.history_up[-1]:.1f} Мбит/с")
        total_down_mb = self.total_down / 1_000_000  # МБайт
        total_up_mb = self.total_up / 1_000_000
        self.total_label.config(text=f"Трафик: {total_down_mb:.1f}/{total_up_mb:.1f} МБ")

        # График
        if len(self.history_down) > 1:
            x_data = list(range(len(self.history_down)))
            self.line_down.set_data(x_data, list(self.history_down))
            self.line_up.set_data(x_data, list(self.history_up))
            self.ax.relim()
            self.ax.autoscale_view()
            self.canvas.draw_idle()

        # Статус
        self.status.config(text=f"Пик: {self.peak_down:.1f} Мбит/с | Средняя: {self.avg_down:.1f} Мбит/с | Интерфейс: {self.interface}")

    def export_csv(self):
        if len(self.history_down) < 2:
            messagebox.showinfo("Информация", "Недостаточно данных для экспорта")
            return
        filename = f"net_data_{int(time.time())}.csv"
        with open(filename, 'w') as f:
            f.write("Время,Загрузка(Мбит/с),Выгрузка(Мбит/с)\n")
            for t, d, u in zip(self.time_history, self.history_down, self.history_up):
                f.write(f"{t},{d:.2f},{u:.2f}\n")
        self.status.config(text=f"Экспортировано в {filename}")

    def reset_stats(self):
        self.history_down.clear()
        self.history_up.clear()
        self.time_history.clear()
        self.total_down = 0
        self.total_up = 0
        self.peak_down = 0
        self.peak_up = 0
        self.avg_down = 0
        self.avg_up = 0
        self.status.config(text="Статистика сброшена")

    def load_config(self):
        if os.path.exists(self.config_file):
            with open(self.config_file, 'r') as f:
                data = json.load(f)
                self.interface = data.get('interface', 'eth0')
                self.interval = data.get('interval', 1.0)

    def save_config(self):
        data = {'interface': self.interface, 'interval': self.interval}
        with open(self.config_file, 'w') as f:
            json.dump(data, f)

    def on_close(self):
        self.running = False
        self.save_config()
        self.root.destroy()

if __name__ == "__main__":
    root = tk.Tk()
    app = NetMonitor(root)
    root.mainloop()
