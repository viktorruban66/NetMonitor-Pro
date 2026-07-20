// netmonitor_cpp.cpp — монитор сети в реальном времени на C++ (Qt Charts)

#include <QApplication>
#include <QMainWindow>
#include <QWidget>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QLabel>
#include <QPushButton>
#include <QTimer>
#include <QChartView>
#include <QLineSeries>
#include <QValueAxis>
#include <QChart>
#include <QMessageBox>
#include <QFile>
#include <QTextStream>
#include <QDateTime>
#include <random>
#include <deque>
#include <QThread>
#include <QNetworkInterface>
#include <QNetworkInformation>

class NetMonitor : public QMainWindow {
    Q_OBJECT
public:
    NetMonitor(QWidget *parent = nullptr) : QMainWindow(parent) {
        setWindowTitle("📊 NetMonitor Pro — C++");
        resize(800, 600);

        // Данные
        downHistory.resize(60);
        upHistory.resize(60);
        timeHistory.resize(60);

        // График
        chart = new QtCharts::QChart();
        chart->setTitle("Скорость сети");
        chart->legend()->show();
        chart->setAnimationOptions(QtCharts::QChart::SeriesAnimations);

        downSeries = new QtCharts::QLineSeries();
        downSeries->setName("Загрузка");
        downSeries->setColor(Qt::blue);
        upSeries = new QtCharts::QLineSeries();
        upSeries->setName("Выгрузка");
        upSeries->setColor(Qt::red);

        axisX = new QtCharts::QValueAxis();
        axisX->setTitleText("Время (сек)");
        axisX->setRange(0, 60);
        axisX->setLabelFormat("%d");

        axisY = new QtCharts::QValueAxis();
        axisY->setTitleText("Скорость (Мбит/с)");
        axisY->setRange(0, 105);

        chart->addSeries(downSeries);
        chart->addSeries(upSeries);
        chart->setAxisX(axisX, downSeries);
        chart->setAxisY(axisY, downSeries);
        chart->setAxisX(axisX, upSeries);
        chart->setAxisY(axisY, upSeries);

        chartView = new QtCharts::QChartView(chart);
        chartView->setRenderHint(QPainter::Antialiasing);

        // UI
        QWidget *central = new QWidget(this);
        setCentralWidget(central);
        QVBoxLayout *mainLayout = new QVBoxLayout(central);

        // Информация
        QHBoxLayout *infoLayout = new QHBoxLayout();
        downLabel = new QLabel("⬇ Загрузка: 0.0 Мбит/с");
        upLabel = new QLabel("⬆ Выгрузка: 0.0 Мбит/с");
        totalLabel = new QLabel("Трафик: 0/0 МБ");
        infoLayout->addWidget(downLabel);
        infoLayout->addWidget(upLabel);
        infoLayout->addWidget(totalLabel);
        mainLayout->addLayout(infoLayout);

        // График
        mainLayout->addWidget(chartView);

        // Кнопки
        QHBoxLayout *btnLayout = new QHBoxLayout();
        QPushButton *exportBtn = new QPushButton("Экспорт CSV");
        QPushButton *resetBtn = new QPushButton("Сброс статистики");
        btnLayout->addWidget(exportBtn);
        btnLayout->addWidget(resetBtn);
        mainLayout->addLayout(btnLayout);

        connect(exportBtn, &QPushButton::clicked, this, &NetMonitor::exportCSV);
        connect(resetBtn, &QPushButton::clicked, this, &NetMonitor::resetStats);

        // Таймер
        timer = new QTimer(this);
        connect(timer, &QTimer::timeout, this, &NetMonitor::updateData);
        timer->start(1000);

        // Инициализация
        totalDown = 0;
        totalUp = 0;
        peakDown = 0;
        peakUp = 0;
        gen = std::mt19937(rd());
        downDist = std::uniform_real_distribution<>(2, 15);
        upDist = std::uniform_real_distribution<>(1, 8);
        // Первое заполнение
        for (int i = 0; i < 10; ++i) {
            downHistory.push_back(0);
            upHistory.push_back(0);
            timeHistory.push_back(0);
        }
    }

private slots:
    void updateData() {
        // Симуляция данных (в реальном проекте — считывание сетевого интерфейса)
        double down = downDist(gen);
        double up = upDist(gen);

        // Обновление истории
        downHistory.push_back(down);
        upHistory.push_back(up);
        timeHistory.push_back(QDateTime::currentSecsSinceEpoch());
        if (downHistory.size() > 60) {
            downHistory.pop_front();
            upHistory.pop_front();
            timeHistory.pop_front();
        }

        totalDown += down;
        totalUp += up;
        if (down > peakDown) peakDown = down;
        if (up > peakUp) peakUp = up;

        // Обновление графиков
        downSeries->clear();
        upSeries->clear();
        for (int i = 0; i < downHistory.size(); ++i) {
            downSeries->append(i, downHistory[i]);
            upSeries->append(i, upHistory[i]);
        }

        // Метки
        downLabel->setText(QString("⬇ Загрузка: %1 Мбит/с").arg(down, 0, 'f', 1));
        upLabel->setText(QString("⬆ Выгрузка: %1 Мбит/с").arg(up, 0, 'f', 1));
        totalLabel->setText(QString("Трафик: %1/%2 МБ").arg(totalDown/8, 0, 'f', 1).arg(totalUp/8, 0, 'f', 1));
    }

    void exportCSV() {
        if (downHistory.size() < 2) {
            QMessageBox::information(this, "Информация", "Недостаточно данных для экспорта");
            return;
        }
        QString filename = QString("net_data_%1.csv").arg(QDateTime::currentSecsSinceEpoch());
        QFile file(filename);
        if (file.open(QIODevice::WriteOnly | QIODevice::Text)) {
            QTextStream out(&file);
            out << "Время,Загрузка(Мбит/с),Выгрузка(Мбит/с)\n";
            for (int i = 0; i < downHistory.size(); ++i) {
                out << timeHistory[i] << "," << downHistory[i] << "," << upHistory[i] << "\n";
            }
            file.close();
            QMessageBox::information(this, "Экспорт", "Данные сохранены в " + filename);
        }
    }

    void resetStats() {
        downHistory.clear();
        upHistory.clear();
        timeHistory.clear();
        totalDown = 0;
        totalUp = 0;
        peakDown = 0;
        peakUp = 0;
        downSeries->clear();
        upSeries->clear();
        // Заполняем нулями
        for (int i = 0; i < 10; ++i) {
            downHistory.push_back(0);
            upHistory.push_back(0);
            timeHistory.push_back(0);
        }
    }

private:
    std::deque<double> downHistory, upHistory;
    std::deque<qint64> timeHistory;
    double totalDown, totalUp;
    double peakDown, peakUp;
    QLabel *downLabel, *upLabel, *totalLabel;
    QtCharts::QChartView *chartView;
    QtCharts::QChart *chart;
    QtCharts::QLineSeries *downSeries, *upSeries;
    QtCharts::QValueAxis *axisX, *axisY;
    QTimer *timer;
    std::random_device rd;
    std::mt19937 gen;
    std::uniform_real_distribution<> downDist, upDist;
};

int main(int argc, char *argv[]) {
    QApplication app(argc, argv);
    NetMonitor w;
    w.show();
    return app.exec();
}

#include "netmonitor_cpp.moc"
