import unittest

class Car:
    def __init__(self, year, make, speed):
        self.__year_model = year
        self.__make = make
        self.__speed = 0

    def set_year_model(self, year):
        self.__year_model = year

    def set_make(self, make):
        self.__make = make

    def set_speed(self, speed):
        self.__speed = 0

    def get_year_model(self):
        return self.__year_model

    def get_make(self):
        return self.__make

    def get_speed(self):
        return self.__speed

    #methods
    def accelerate(self):
        self.__speed +=5

    def brake(self):
        self.__speed -=5

    def get_speed(self):
        return self.__speed

def main():

    year = 2000
    make = "ford"
    speed = 0

    mycar = Car(year, make, speed)

    #Accelerate 5 times
    mycar.accelerate()
    print('The current speed is: ', mycar.get_speed())
    mycar.accelerate()
    print('The current speed is: ', mycar.get_speed())
    mycar.accelerate()
    print('The current speed is: ', mycar.get_speed())
    mycar.accelerate()
    print('The current speed is: ', mycar.get_speed())
    mycar.accelerate()
    print('The current speed is: ', mycar.get_speed()) 

    #Brake 5 times
    mycar.brake()
    print('The current speed after brake is: ', mycar.get_speed())
    mycar.brake()
    print('The current speed after brake is: ', mycar.get_speed())
    mycar.brake()
    print('The current speed after brake is: ', mycar.get_speed())
    mycar.brake() 
    print('The current speed after brake is: ', mycar.get_speed())
    mycar.brake()
    print('The current speed after brake is: ', mycar.get_speed())

#Call the main function
main()

class TestCar(unittest.TestCase):
      def setUp(self):
          self.car = Car()


class TestInit(TestCar):
      def test_initial_speed(self):
          self.assertEqual(self.car.speed, 0)

      def test_initial_odometer(self):
          self.assertEqual(self.car.odometer, 0)

      def test_initial_time(self):
          self.assertEqual(self.car.time, 0)


class TestAccelerate(TestCar):
      def test_accelerate_from_zero(self):
          self.car.accelerate()
          self.assertEqual(self.car.speed, 5)

      def test_multiple_accelerates(self):
          for _ in range(3):
            self.car.accelerate()
          self.assertEqual(self.car.speed, 15)


class TestBrake(TestCar):
       def test_brake_once(self):
           self.car.accelerate()
           self.car.brake()
           self.assertEqual(self.car.speed, 0)

       def test_multiple_brakes(self):
            for _ in range(5):
                self.car.accelerate()
            for _ in range(3):
                self.car.brake()
            self.assertEqual(self.car.speed, 10)

       def test_should_not_allow_negative_speed(self):
           self.car.brake()
           self.assertEqual(self.car.speed, 0)

       def test_multiple_brakes_at_zero(self):
           for _ in range(3):
               self.car.brake()
           self.assertEqual(self.car.speed, 0)
