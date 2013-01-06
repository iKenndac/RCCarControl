//
//  DKCarOrientationView.m
//  RCDistanceVisualiser
//
//  Created by Daniel Kennett on 06/01/2013.
//  Copyright (c) 2013 Daniel Kennett. All rights reserved.
//

#import "DKRCCarOrientationView.h"

@implementation DKRCCarOrientationView

-(void)drawRect:(NSRect)dirtyRect {
	NSRect leftBounds = self.bounds;
	leftBounds.size.width = leftBounds.size.width / 2.0;
	NSRect bounds = self.bounds;
	[[NSColor whiteColor] set];
	NSRectFill(bounds);

	// Z is up-down (positive when the car is right-way up)
	// Y is front-back (acceleration and braking)
	// X is left-right (roll)

	// http://stackoverflow.com/questions/7522436/ios-accelerometer-gyroscope-question

	double yValue = self.z;
	double xValue = self.y;
	double radians = atan2(yValue, xValue);
	double degrees = radians * (180.0 / M_PI);
	degrees -= 90.0;
	degrees = 360.0 - degrees;
	
	double correctedRadians = degrees * (M_PI / 180.0);
		
	NSImage *carImage = [NSImage imageNamed:@"car-side"];
	NSSize carSize = carImage.size;
	NSRect carRect = NSMakeRect(NSMidX(leftBounds) - (carSize.width / 2),
								NSMidY(leftBounds) - (carSize.height / 2),
								carSize.width,
								carSize.height);
	
	carRect.origin.x = floor(carRect.origin.x);
	carRect.origin.y = floor(carRect.origin.y);
	
	NSPoint translatePoint = NSMakePoint(NSMidX(carRect), NSMidY(carRect));
	
	NSAffineTransform *transform = [NSAffineTransform transform];
	if (self.x != 0.0 || self.y != 0.0 || self.z != 0.0) {
		[transform translateXBy:translatePoint.x yBy:translatePoint.y];
		[transform rotateByRadians:correctedRadians];
		[transform translateXBy:-translatePoint.x yBy:-translatePoint.y];
	}
	
	[NSGraphicsContext saveGraphicsState];
	[transform concat];

	[carImage drawInRect:carRect
				fromRect:NSZeroRect
			   operation:NSCompositeSourceOver
				fraction:1.0];

	[NSGraphicsContext restoreGraphicsState];
	
	// Rear
	
	NSRect rightBounds = self.bounds;
	rightBounds.size.width = rightBounds.size.width / 2.0;
	rightBounds.origin.x += rightBounds.size.width;

	double rearYValue = self.z;
	double rearXValue = self.x;
	double rearRadians = atan2(rearYValue, rearXValue);
	double rearDegrees = rearRadians * (180.0 / M_PI);
	rearDegrees -= 90.0;
	rearDegrees = 360.0 - rearDegrees;
	while (rearDegrees > 360.0) rearDegrees -= 360.0;
	
	double correctedRearRadians = rearDegrees * (M_PI / 180.0);
	
	NSImage *carRearImage = [NSImage imageNamed:@"car-rear"];
	NSSize carRearSize = carRearImage.size;
	NSRect carRearRect = NSMakeRect(NSMidX(rightBounds) - (carRearSize.width / 2),
									NSMidY(rightBounds) - (carRearSize.height / 2),
									carRearSize.width,
									carRearSize.height);
	
	carRearRect.origin.x = floor(carRearRect.origin.x);
	carRearRect.origin.y = floor(carRearRect.origin.y);
	
	NSPoint translatePointRear = NSMakePoint(NSMidX(carRearRect), NSMidY(carRearRect));
	
	NSAffineTransform *transformRear = [NSAffineTransform transform];
	if (self.x != 0.0 || self.y != 0.0 || self.z != 0.0) {
		[transformRear translateXBy:translatePointRear.x yBy:translatePointRear.y];
		[transformRear rotateByRadians:correctedRearRadians];
		[transformRear translateXBy:-translatePointRear.x yBy:-translatePointRear.y];
	}
	
	[NSGraphicsContext saveGraphicsState];
	[transformRear concat];
	
	[carRearImage drawInRect:carRearRect
					fromRect:NSZeroRect
				   operation:NSCompositeSourceOver
					fraction:1.0];
	
	[NSGraphicsContext restoreGraphicsState];
	
	if (rearDegrees > 130.0 && rearDegrees < 360.0 - 130.0) {
		
		NSMutableParagraphStyle *paragraphStyle = [[NSParagraphStyle defaultParagraphStyle] mutableCopy];
		paragraphStyle.alignment = NSCenterTextAlignment;
		
		NSDictionary *attributes = @{ NSForegroundColorAttributeName : [NSColor redColor],
		NSFontAttributeName : [NSFont boldSystemFontOfSize:[NSFont smallSystemFontSize]],
		NSParagraphStyleAttributeName : paragraphStyle };
		
		
		
		NSString *warning = @"WARNING: Vehicle appears to be upside-down.";
		[warning drawInRect:NSMakeRect(0.0, 0.0, NSWidth(bounds), 30.0)
			 withAttributes:attributes];
		
	}

}

@end
