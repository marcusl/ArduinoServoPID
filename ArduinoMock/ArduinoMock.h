#pragma once

// ReSharper disable CppInconsistentNaming
// ReSharper disable CppMemberFunctionMayBeStatic
// ReSharper disable CppParameterNeverUsed

constexpr const char* F(const char* x) { return x; }

// Mock globals

namespace mock
{
MOCK_API extern unsigned long    gMicros;
MOCK_API extern std::vector<int> gAnalogPins;
}

inline unsigned long micros() { return mock::gMicros; }
inline void          setMockMicros(const unsigned long value) { mock::gMicros = value; }

inline void delay(int ms)
{
};

inline int analogRead(const int pin) { return mock::gAnalogPins.at(pin); }

inline void setMockAnalogRead(const int pin, const int value)
{
  mock::gAnalogPins.at(pin) = value;
}

template <typename T, typename U>
T constrain(const T v, const U min, const U max)
{
  if (v < T(min))
    return T(min);
  if (v > T(max))
    return T(max);
  return v;
}

// Mock Servo

class MockServo
{
public:
  void attach(const int pin, const int min, const int max)
  {
    _pin = pin;
    _min = min;
    _max = max;
  }

  void write(const int angle) { _angle = angle; }

  int angle() const { return _angle; }

private:
  int _pin = 0;
  int _min = 0;
  int _max = 0;

  int _angle = 0;
};

typedef MockServo Servo;


class MockSerial
{
public:
  void begin(int baudRate) const
  {
  }

  void end() const
  {
  }

  int available() const { return _dataToArduino.str().size(); }

  char read()
  {
    if (_dataToArduino.str().empty())
      return 0;

    char value;
    _dataToArduino >> value;
    return value;
  }

  void setMockData(const std::string& str)
  {
    _dataToArduino.clear();
    _dataToArduino << str;
  }

  void setMockData(const std::vector<char>& data)
  {
    setMockData(std::string(&data.front(), data.size()));
  }

  template <typename T>
  void println(T value, int format = 3)
  {
    _dataFromArduino << value;
    notify();
  }

  template <typename T>
  void print(T value, int format = 3)
  {
    _dataFromArduino << value;
    notify();
  }

  void flush()
  {
  }

  void resetMock()
  {
    _dataToArduino.clear();
    _dataFromArduino.clear();
    _dataFromArduino.str({});
  }

  void setCallback(void (*callback)()) { _callback = callback; }

  template <class T>
  void writeMock(const T& value)
  {
    _dataToArduino << value;
  }

  template <class T>
  void readMock(T& value)
  {
    _dataFromArduino >> value;
  }

  std::string readMockLine()
  {
    std::string resp;
    std::getline(_dataFromArduino, resp);
    return resp;
  }

private:
  void notify() const
  {
    if (_callback)
      _callback();
  }

  std::stringstream _dataToArduino;
  std::stringstream _dataFromArduino;
  void (*           _callback)() = nullptr;
};

MOCK_API extern MockSerial Serial;

class MockWire
{
public:
  void begin() const
  {
  }

  void setClock(const int baudRate) const
  {
  }
};

MOCK_API extern MockWire Wire;


class MockEEPROM
{
public:
  MockEEPROM()
  {
    _mem.fill(0);
  }

  uint8_t read(int addr) { return _mem[addr]; }

  void write(int addr, uint8_t value) { _mem[addr] = value; }
  void update(int addr, uint8_t value) { write(addr, value); }

  template <typename T>
  void put(int addr, T value) { reinterpret_cast<T&>(_mem[addr]) = value; }

  template <typename T>
  void get(int addr, T& value) { value = reinterpret_cast<const T&>(_mem[addr]); }

  uint8_t& operator[](int addr) { return _mem[addr]; }

  constexpr unsigned int length() const { return _mem.size(); }

  std::array<uint8_t, 4096> _mem{};
};

MOCK_API extern MockEEPROM EEPROM;

// ReSharper restore CppInconsistentNaming
// ReSharper restore CppMemberFunctionMayBeStatic
// ReSharper restore CppParameterNeverUsed