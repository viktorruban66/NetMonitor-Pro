// netmonitor_rs.rs — монитор сети в реальном времени на Rust (консоль + termion)

use rand::Rng;
use std::io::{self, Write, BufRead};
use std::time::{Duration, SystemTime, UNIX_EPOCH};
use std::thread;
use termion::{color, style, cursor, clear};

struct NetMonitor {
    down_history: Vec<f64>,
    up_history: Vec<f64>,
    time_history: Vec<u64>,
    total_down: f64,
    total_up: f64,
    peak_down: f64,
    peak_up: f64,
    history_size: usize,
}

impl NetMonitor {
    fn new() -> Self {
        let mut n = NetMonitor {
            down_history: Vec::new(),
            up_history: Vec::new(),
            time_history: Vec::new(),
            total_down: 0.0,
            total_up: 0.0,
            peak_down: 0.0,
            peak_up: 0.0,
            history_size: 60,
        };
        for _ in 0..10 {
            n.down_history.push(0.0);
            n.up_history.push(0.0);
            n.time_history.push(Self::current_time());
        }
        n
    }

    fn current_time() -> u64 {
        SystemTime::now().duration_since(UNIX_EPOCH).unwrap().as_secs()
    }

    fn update(&mut self) {
        let mut rng = rand::thread_rng();
        let down = rng.gen_range(2.0..15.0);
        let up = rng.gen_range(1.0..8.0);

        self.down_history.push(down);
        self.up_history.push(up);
        self.time_history.push(Self::current_time());
        if self.down_history.len() > self.history_size {
            self.down_history.remove(0);
            self.up_history.remove(0);
            self.time_history.remove(0);
        }
        self.total_down += down;
        self.total_up += up;
        if down > self.peak_down {
            self.peak_down = down;
        }
        if up > self.peak_up {
            self.peak_up = up;
        }
    }

    fn display(&self) {
        print!("{}{}", clear::All, cursor::Goto(1, 1));
        println!("📊 NetMonitor Pro — Rust Edition");
        if let Some(&last_down) = self.down_history.last() {
            println!("⬇ Загрузка: {:.1} Мбит/с", last_down);
        }
        if let Some(&last_up) = self.up_history.last() {
            println!("⬆ Выгрузка: {:.1} Мбит/с", last_up);
        }
        println!("Трафик: {:.1}/{:.1} МБ", self.total_down/8.0, self.total_up/8.0);
        println!("Пик: {:.1} Мбит/с   Средняя: {:.1} Мбит/с", self.peak_down, self.average(&self.down_history));

        // ASCII график
        println!("\nГрафик скорости за последние 60 сек (синий — загрузка, красный — выгрузка):");
        if self.down_history.len() > 1 {
            const WIDTH: usize = 50;
            const HEIGHT: usize = 10;
            let max_val = self.down_history.iter().chain(self.up_history.iter()).fold(10.0, |a, &b| a.max(b));
            let data_down = &self.down_history;
            let data_up = &self.up_history;
            let step = (data_down.len() - 1) as f64 / (WIDTH - 1) as f64;

            let mut grid_down = vec![vec![' '; WIDTH]; HEIGHT];
            let mut grid_up = vec![vec![' '; WIDTH]; HEIGHT];
            for i in 0..WIDTH {
                let idx = (i as f64 * step) as usize;
                let idx = if idx >= data_down.len() { data_down.len() - 1 } else { idx };
                let val_down = data_down[idx];
                let val_up = data_up[idx];
                let y_down = ((val_down / max_val) * (HEIGHT - 1) as f64) as usize;
                let y_up = ((val_up / max_val) * (HEIGHT - 1) as f64) as usize;
                let y_down = if y_down >= HEIGHT { HEIGHT - 1 } else { y_down };
                let y_up = if y_up >= HEIGHT { HEIGHT - 1 } else { y_up };
                grid_down[HEIGHT - 1 - y_down][i] = '█';
                grid_up[HEIGHT - 1 - y_up][i] = '█';
            }
            print!("      ");
            for j in 0..WIDTH {
                if j % 5 == 0 { print!("{}", j/5); } else { print!(" "); }
            }
            println!();
            for i in 0..HEIGHT {
                print!("{:3}% ", (HEIGHT-1-i)*100/(HEIGHT-1));
                for j in 0..WIDTH {
                    let c = if grid_down[i][j] == '█' && grid_up[i][j] == '█' {
                        format!("{}█{}", color::Fg(color::Magenta), style::Reset)
                    } else if grid_down[i][j] == '█' {
                        format!("{}█{}", color::Fg(color::Blue), style::Reset)
                    } else if grid_up[i][j] == '█' {
                        format!("{}█{}", color::Fg(color::Red), style::Reset)
                    } else {
                        " ".to_string()
                    };
                    print!("{}", c);
                }
                println!();
            }
        }
        println!("\nКоманды: export, reset, exit");
        print!("> ");
        io::stdout().flush().unwrap();
    }

    fn average(&self, data: &[f64]) -> f64 {
        if data.is_empty() { 0.0 } else { data.iter().sum::<f64>() / data.len() as f64 }
    }

    fn export_csv(&self) {
        if self.down_history.len() < 2 {
            println!("Недостаточно данных для экспорта");
            return;
        }
        let filename = format!("net_data_{}.csv", Self::current_time());
        if let Ok(mut file) = std::fs::File::create(&filename) {
            use std::io::Write;
            writeln!(file, "Время,Загрузка(Мбит/с),Выгрузка(Мбит/с)").unwrap();
            for i in 0..self.down_history.len() {
                writeln!(file, "{},{:.2},{:.2}", self.time_history[i], self.down_history[i], self.up_history[i]).unwrap();
            }
            println!("Данные сохранены в {}", filename);
        } else {
            println!("Ошибка создания файла");
        }
    }

    fn reset(&mut self) {
        self.down_history.clear();
        self.up_history.clear();
        self.time_history.clear();
        self.total_down = 0.0;
        self.total_up = 0.0;
        self.peak_down = 0.0;
        self.peak_up = 0.0;
        for _ in 0..10 {
            self.down_history.push(0.0);
            self.up_history.push(0.0);
            self.time_history.push(Self::current_time());
        }
        println!("Статистика сброшена");
    }
}

fn main() {
    let mut monitor = NetMonitor::new();
    let stdin = io::stdin();
    let mut reader = stdin.lock();
    println!("📊 NetMonitor Pro — Rust Edition");
    println!("Нажмите Enter для обновления, или введите команду: export, reset, exit");
    loop {
        monitor.update();
        monitor.display();
        let mut input = String::new();
        if reader.read_line(&mut input).is_err() { break; }
        let input = input.trim();
        match input {
            "export" => monitor.export_csv(),
            "reset" => monitor.reset(),
            "exit" => {
                println!("До свидания!");
                break;
            }
            _ => {}
        }
        thread::sleep(Duration::from_millis(100));
    }
}
