﻿using FlightManager.Data;
using FlightManager.Data.Models;
using FlightManager.Services.Contracts;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightManager.Services
{
    public class ReservationService : IReservationService
    {
        private readonly ApplicationDbContext dBContext;
        private readonly IEmailSender emailSender;

        public ReservationService(ApplicationDbContext context, IEmailSender emailSender)
        {
            dBContext = context;
            this.emailSender = emailSender;
        }

        public FlightBooking ChangeTicketType(FlightBooking reservation)
        {
            dBContext.FlightBookings.Where(r => r.Id == reservation.Id).First().TicketType = reservation.TicketType == "Business" ? "Regular" : "Business";
            dBContext.SaveChanges();

            return reservation;
        }

        public List<FlightBooking> GetAllReservations()
        {
            return dBContext.FlightBookings.ToList();
        }

        public List<FlightBooking> GetAllReservationsForFlight(Flight flight)
        {
            return dBContext.FlightBookings.Where(r => r.Flight.Id == flight.Id).ToList();
        }

        public FlightBooking CreateReservation(ReservationCreateViewModel model)
        {
            
            FlightBooking reservation = new FlightBooking()
            {
                TicketType = model.TicketType,
                Flight = dBContext.Flights.Where(f => f.Id == model.FlightId).First(),
                IsConfirmed = false,
                TicketsCount =1,
                Email = model.Email,
                FirstName = model.FirstName,
                MiddleName = model.SecondName,
                Surname = model.LastName,
                SSN = model.SSN,
                PhoneNumber = model.PhoneNumber,
                Nationality = model.Nationality,
                FlightID = model.FlightId

            };

            Flight dbFlight = dBContext.Flights.Where(f => f.Id.ToString() == model.FlightId.ToString()).First();
            if (model.TicketType == "Business")
            {
                dbFlight.BusinessTicketsLeft -= model.TicketCount;
            }
            else
            {
                dbFlight.TicketsLeft -= model.TicketCount;
            }
            
            dBContext.FlightBookings.Add(reservation);
            dBContext.SaveChanges();

            string msg = $@"Thank you for using our services! <br> You have successfuly made a reservation 
            for flight {dbFlight.Id} from {dbFlight.LeavingFrom} to {dbFlight.GoingTo} leaving on {dbFlight.Departure}.
                            Now it's only left for you to confirm it. <br />
                            <a href={"https://localhost:44378"}/FlightBookings/Confirm?id={reservation.Id}>Confirm Reservation</a> <br />
                            <a href={"https://localhost:44378"}/FlightBookings/Delete?id={reservation.Id}>Delete</a>";

            emailSender.SendEmailAsync(reservation.Email, "Reservation Confirmation", msg).GetAwaiter().GetResult();

            return reservation;
        }

        public FlightBooking ConfirmReservation(int id) { 
            FlightBooking reservation = dBContext.FlightBookings.Where(r => r.Id == id).First();
            reservation.IsConfirmed = true;

            dBContext.FlightBookings.Update(reservation);
            dBContext.SaveChanges();

            return reservation;
        }

        public FlightBooking DeleteReservation(int id)
        {
            FlightBooking reservation = dBContext.FlightBookings.Where(r => r.Id == id).First();
            
            if (reservation.IsConfirmed) {
                string msg = $@"We are sorry to inform you that there have been some issues 
                 with flight No. {reservation.Flight.Id} - {reservation.Flight.LeavingFrom} 
                 to {reservation.Flight.GoingTo}, which led to its cancelation and the termination of your reservation. <br>
                 Please contact us to choose another flight to book 
                 or to have your money returned. <br> <br>
                 Thank you for using our services! <br> <br> Cloud Express";

                emailSender.SendEmailAsync(reservation.Email, "Canceled Reservation", msg).GetAwaiter().GetResult();
            }
            Flight dbFlight = dBContext.Flights.Where(f => f.Id.ToString() == reservation.Flight.Id.ToString()).First();
            if (reservation.TicketType == "Business")
            {
                dbFlight.BusinessTicketsLeft += 1;
            }
            else
            {
                dbFlight.TicketsLeft +=1;
            }


            dBContext.FlightBookings.Remove(reservation);
            dBContext.SaveChanges();

            return reservation;
        }

        public FlightBooking GetReservationById(int id)
        {
            FlightBooking reservation = dBContext.FlightBookings.Where(r => r.Id == id).First();
            reservation.Flight = dBContext.Flights.Where(f => f.Id == reservation.FlightID).First();
            return reservation;
        }
    }
}
